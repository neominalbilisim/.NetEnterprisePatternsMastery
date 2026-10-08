namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "admin";
    public string Password { get; init; } = "admin";
}
