# Enterprise Patterns Sample (.NET 8)

Sipariş (**Ordering**) ve Stok (**Inventory**) servislerinden oluşan örnek bir monorepo. Amaç, kurumsal uygulamalarda sık kullanılan mimari yaklaşımları ve tasarım kalıplarını **birlikte ve uçtan uca** çalışır halde göstermektir.

> Kodlar İngilizce, açıklamalar (yorumlar) Türkçedir.

## Özellikler

| Konu | Uygulama |
|---|---|
| **Hexagonal + Clean Architecture** | Her servis `Domain → Application → Infrastructure → Api` katmanlarına ayrılır. Bağımlılıklar içe doğrudur. Application katmanı *port*'ları tanımlar, Infrastructure bunları *adapter* olarak uygular. |
| **Monorepo + Abstraction Core** | Ortak yapılar `src/BuildingBlocks` altında toplanır. `BuildingBlocks.Abstractions` altyapıdan bağımsız çekirdek sözleşmeleri içerir. |
| **CQRS (MediatR)** | Yazma tarafında `ICommand` ve EF Core, okuma tarafında `IQuery` ve Dapper kullanılır. |
| **Pipeline Behaviors** | Sıra: Logging → Validation → Transaction → Idempotency → Inbox → Handler |
| **Result Pattern** | Beklenen hatalar exception fırlatmak yerine `Result`/`Error` ile döner. Bu hatalar HTTP'de ProblemDetails'e, gRPC'de `StatusCode`'a çevrilir. |
| **Repository + Specification** | Repository'ler yalnızca aggregate root'lar içindir ve `IQueryable` sızdırmaz. Sorgu kriterleri Specification ile kapsüllenir ve `And`/`Or` ile birleştirilebilir. |
| **Unit of Work** | `SaveChanges` sırasında domain event'ler aynı transaction içinde dispatch edilir. |
| **Domain Event / Integration Event** | Domain event in-process çalışır. İlgili handler bunu bir integration event'e çevirerek Outbox'a yazar. |
| **Outbox (DotNetCore.CAP)** | İş verisi ile mesaj aynı Postgres transaction'ında commit edilir; mesaj daha sonra RabbitMQ'ya iletilir. |
| **Idempotency** | Üç katmanda sağlanır: API'de `Idempotency-Key` header'ı, consumer'da Inbox tablosu, iş kuralında `UNIQUE(order_id)`. |
| **Resiliency** | HTTP tarafında retry (exponential + jitter), timeout ve circuit breaker kullanılır. gRPC tarafında native retry policy ve deadline, mesajlaşmada CAP retry, veritabanında optimistic concurrency (`xmin`), başlangıçta ise Polly retry vardır. |
| **Senkron iletişim (HTTP + gRPC)** | Ordering, müşteri bilgisini Legacy ERP'den HTTP ile, stok ve fiyat bilgisini Inventory'den gRPC ile alır. |
| **Anti-Corruption Layer** | Legacy ERP'nin `CUST_NO`, `STAT_CD`, `"50.000,00"` gibi alanlarından oluşan modeli, `Adapters/LegacyErp` altında bizim modelimize çevrilir. |
| **Loglama** | Serilog ile yapısal log üretilir ve Console ile Seq'e yazılır. Her log kaydında `Application` alanı bulunur. |
| **Veritabanı** | PostgreSQL kullanılır; her servisin kendi veritabanı vardır (database-per-service). |

## Mimari

```
                          HTTP (ACL + Polly)          ┌────────────────────┐
                    ┌────────────────────────────────►│ LegacyErp.Mock     │
                    │                                 │ (eski tip model,   │
┌────────┐  REST    │                                 │  chaos ayarları)   │
│ Client ├────►┌────┴──────────────┐  gRPC (retry)    └────────────────────┘
│Postman │     │   Ordering.Api    ├─────────────────►┌────────────────────┐
└────────┘     │ Domain/App/Infra  │                  │   Inventory.Api    │
               └───┬───────────▲───┘                  │ REST :5002         │
                   │ Outbox    │ Inbox                │ gRPC :5102         │
                   ▼           │                      └───▲───────────┬────┘
            ┌──────────────────┴─────────────────────────┴───────────▼──┐
            │      RabbitMQ (CAP)   order.created ─►   ◄─ stock.*        │
            └──────────────────────────────────────────────────────────┘
       ordering_db (Postgres)                         inventory_db (Postgres)
```

### Uçtan uca akış

1. `POST /api/orders` isteği `Idempotency-Key` header'ı ile gelir ve pipeline'dan geçer (Validation, Transaction, Idempotency).
2. **ACL/HTTP:** Müşterinin durumu ve kullanılabilir kredisi Legacy ERP'den okunur.
3. **Specification:** Müşterinin bekleyen sipariş sayısı sınırı kontrol edilir.
4. **gRPC:** Inventory'den stok ve güncel fiyat alınır.
5. `Order.Create` çağrılır ve `OrderCreatedDomainEvent` üretilir. Handler bunu `OrderCreatedIntegrationEvent`'e çevirip `cap.published` tablosuna (Outbox) yazar. Tümü tek transaction içinde commit edilir.
6. CAP mesajı RabbitMQ'ya iletir. Inventory tarafında Inbox kontrolünden sonra stok rezerve edilir ya da rezervasyon reddedilir; sonuç yine Outbox üzerinden `StockReserved` veya `StockReservationFailed` olarak yayınlanır.
7. Ordering bu event'i Inbox kontrolünden sonra tüketir ve siparişi `Confirmed` ya da `Rejected` durumuna çeker (compensation).

## Klasör Yapısı

```
├── src
│   ├── BuildingBlocks
│   │   ├── BuildingBlocks.Abstractions     # Entity, AggregateRoot, Result, Specification, IRepository, IUnitOfWork, CQRS arayüzleri
│   │   ├── BuildingBlocks.Application      # Pipeline behavior'lar, MediatR/FluentValidation kaydı
│   │   └── BuildingBlocks.Infrastructure   # EF/CAP UnitOfWork, EfRepository, Inbox/Idempotency store, Serilog, ProblemDetails
│   ├── Contracts
│   │   ├── Contracts.IntegrationEvents     # Servisler arası event sözleşmeleri ve topic isimleri
│   │   └── Contracts.Grpc                  # inventory.proto (client + server aynı tipleri kullanır)
│   ├── Services
│   │   ├── Ordering  (Domain / Application / Infrastructure / Api)
│   │   └── Inventory (Domain / Application / Infrastructure / Api)
│   └── ExternalSystems
│       └── LegacyErp.Mock                  # ACL ve resiliency senaryoları için sahte ERP
├── postman/EnterprisePatterns.postman_collection.json
├── docker-compose.yml
├── Directory.Build.props / Directory.Packages.props   # Ortak ayarlar ve Central Package Management
└── EnterprisePatterns.sln
```

### Kalıpların kodda yerleri

| Kalıp | Dosya |
|---|---|
| Pipeline behavior'lar | `BuildingBlocks.Application/Behaviors/*` |
| Result / Error | `BuildingBlocks.Abstractions/Results/*` |
| Specification ve birleştirme | `BuildingBlocks.Abstractions/Specifications/*`, `Ordering.Domain/Orders/Specifications` |
| Unit of Work + CAP transaction + domain event dispatch | `BuildingBlocks.Infrastructure/Persistence/UnitOfWork.cs` |
| Outbox publisher | `BuildingBlocks.Infrastructure/Messaging/CapIntegrationEventPublisher.cs` |
| Inbox (idempotent consumer) | `InboxBehavior.cs`, `*/Subscribers/*.cs` |
| Anti-Corruption Layer | `Ordering.Infrastructure/Adapters/LegacyErp/*` |
| HTTP resiliency | `Ordering.Infrastructure/DependencyInjection.cs` → `AddStandardResilienceHandler` |
| gRPC client/server | `Ordering.Infrastructure/Adapters/Grpc`, `Inventory.Api/Grpc` |
| Dapper read model | `*/Infrastructure/Persistence/ReadModels/*` |
| Optimistic concurrency | `Inventory.Infrastructure/.../InventoryConfigurations.cs` (`xmin`) |

## Çalıştırma

**Gereksinimler:** .NET 8 SDK ve Docker.

### Seçenek 1 – Her şey Docker'da

```bash
docker compose up --build
```

Bu modda chaos ayarları açıktır: ERP isteklerinin %20'si 503 döner, gRPC çağrılarının %20'si `Unavailable` ile reddedilir. Böylece retry mekanizmaları Seq'te doğrudan gözlemlenebilir.

### Seçenek 2 – Altyapı Docker'da, servisler lokalde

```bash
docker compose up -d postgres rabbitmq seq
dotnet run --project src/ExternalSystems/LegacyErp.Mock
dotnet run --project src/Services/Inventory/Inventory.Api
dotnet run --project src/Services/Ordering/Ordering.Api
```

Veritabanları ve tablolar ilk açılışta otomatik oluşturulur. Inventory için örnek ürünler de eklenir.

### Adresler

| Servis | Adres |
|---|---|
| Ordering API / Swagger | http://localhost:5001/swagger |
| Inventory API / Swagger | http://localhost:5002/swagger |
| Inventory gRPC (HTTP/2) | localhost:5102 (server reflection açık) |
| Legacy ERP Mock | http://localhost:5003 |
| CAP Dashboard | http://localhost:5001/cap · http://localhost:5002/cap |
| Seq | http://localhost:5341 |
| RabbitMQ | http://localhost:15672 (admin/admin) |

### Demo verisi

| Ürün | Id | Fiyat | Stok |
|---|---|---|---|
| Mechanical Keyboard | `1111…1111` | 2.499,90 | 50 |
| Wireless Mouse | `2222…2222` | 899,50 | 100 |
| 27" 4K Monitor | `3333…3333` | 8.999,00 | **3** |

| Müşteri (ERP) | Durum |
|---|---|
| `C-1001` | Aktif, 45.000 kullanılabilir kredi |
| `C-1002` | Bloke (`STAT_CD = B`) |
| `C-1003` | Aktif, yalnızca 500 kullanılabilir kredi |

## Postman

`postman/EnterprisePatterns.postman_collection.json` dosyasını içe aktarın. Klasörler sırasıyla şunları içerir: Inventory, Ordering senaryoları (happy path, idempotent tekrar, yetersiz stok, stok yarışı, bloke müşteri, limit aşımı, validation), ERP chaos/resilience demosu ve gözlem adresleri.

**gRPC:** Postman'in v2.1 collection formatı gRPC isteklerini saklayamaz. Postman'de *New → gRPC* seçip `localhost:5102` adresine bağlanın ve *server reflection* ile `inventory.v1.InventoryService` metodlarını çağırın.

## Resiliency Demosu

1. **Circuit breaker:** `ERP - Chaos: 100% Failure` isteğini çalıştırın, ardından `Create Order - Happy Path` isteğini birkaç kez gönderin. İlk isteklerde retry'lar görülür. Hata oranı %50'yi geçince devre açılır ve yanıtlar anında `503 Customer.ServiceUnavailable` olarak döner. 15 saniye sonra devre half-open durumuna geçer.
2. **Timeout:** `ERP - Chaos: 3s Latency` isteğini çalıştırın. Deneme başına timeout 2 saniye, toplam timeout 10 saniyedir.
3. **gRPC retry:** `Chaos__GrpcFailureRate` açıkken Seq'te `Chaos: simulating gRPC outage` loglarını izleyin; istemci aynı çağrıyı otomatik olarak yeniden dener.
4. **Mesajlaşma:** Inventory'yi durdurup sipariş oluşturun. Mesaj Outbox'ta bekler; servis yeniden açıldığında işlenir ve sipariş onaylanır.

## Bilinçli Tasarım Kararları

- **EnsureCreated:** Örneğin kolay çalışması için migration yerine kullanılmıştır. Gerçek projelerde EF Core Migrations tercih edilmelidir.
- **Transaction içinde uzak çağrı:** `TransactionBehavior`, transaction'ı handler'dan önce açar. Bu yüzden `CreateOrder` içindeki ERP ve gRPC çağrıları açık bir transaction içinde yapılır. Çağrılar kısa timeout'larla sınırlandırılmıştır. Yüksek yük altında bu kontroller transaction dışına taşınabilir.
- **`BeginTransaction` senkron:** CAP, aktif transaction'ı `AsyncLocal` ile taşır. Bu değerin handler'a akabilmesi için metodun senkron olması gerekir (ayrıntı için `UnitOfWork.cs` içindeki yoruma bakın).
- **MediatR 12.x:** Sürüm 13'ten itibaren ticari lisans modeline geçildiği için Apache 2.0 lisanslı 12.4.1 sürümüne sabitlenmiştir.
- **Business failure ≠ retry:** Subscriber'larda iş hatası (`Result.Failure`) loglanır ve mesaj tekrar denenmez. Yalnızca exception'lar (geçici hatalar) CAP tarafından yeniden denenir.
