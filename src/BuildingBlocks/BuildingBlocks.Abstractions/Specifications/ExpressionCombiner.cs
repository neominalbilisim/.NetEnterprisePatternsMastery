using System.Linq.Expressions;

namespace BuildingBlocks.Abstractions.Specifications;

/// <summary>
/// İki lambda ifadesini tek bir parametre üzerinde birleştirir.
/// Parametreleri ortaklaştırmak gerekir; aksi halde EF Core ifadeyi SQL'e çeviremez.
/// </summary>
internal static class ExpressionCombiner
{
    public static Expression<Func<T, bool>>? AndAlso<T>(
        Expression<Func<T, bool>>? left, Expression<Func<T, bool>>? right)
    {
        // Kriter yoksa "tüm kayıtlar" anlamına gelir; AND'de etkisizdir.
        if (left is null) return right;
        if (right is null) return left;
        return Combine(left, right, Expression.AndAlso);
    }

    public static Expression<Func<T, bool>>? OrElse<T>(
        Expression<Func<T, bool>>? left, Expression<Func<T, bool>>? right)
    {
        // Taraflardan biri "tüm kayıtlar" ise OR sonucu da "tüm kayıtlar"dır.
        if (left is null || right is null) return null;
        return Combine(left, right, Expression.OrElse);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
