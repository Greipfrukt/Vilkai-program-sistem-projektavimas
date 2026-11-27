using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Synq.Common.Errors;

namespace Synq.Common.Http;

/// <summary>
/// offset + limit (numatyta 0 / 50, limit ≤ 100). Netinkama reikšmė → 400 BAD_REQUEST (P-02).
/// </summary>
public readonly record struct PageRequest(int Offset, int Limit)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    public static PageRequest From(int? offset, int? limit)
    {
        var o = offset ?? 0;
        var l = limit ?? DefaultLimit;
        if (o < 0)
            throw ApiException.BadRequest("offset must be 0 or greater.");
        if (l is < 1 or > MaxLimit)
            throw ApiException.BadRequest($"limit must be between 1 and {MaxLimit}.");
        return new PageRequest(o, l);
    }
}

/// <summary>Puslapio atsakymas pagal yaml PageMeta + items.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int Offset, int Limit);

public static class PagingExtensions
{
    /// <summary>Suskaičiuoja total ir paima vieną puslapį (užklausa jau turi būti surūšiuota).</summary>
    public static async Task<Page<T>> ToPageAsync<T>(this IQueryable<T> query, PageRequest page, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Offset).Take(page.Limit).ToListAsync(ct);
        return new Page<T>(items, total, page.Offset, page.Limit);
    }

    public static Page<TOut> Map<TIn, TOut>(this Page<TIn> page, Func<TIn, TOut> map) =>
        new(page.Items.Select(map).ToList(), page.Total, page.Offset, page.Limit);
}

/// <summary>
/// Leidžiami rūšiavimo laukai vienam endpoint'ui ir <c>sort=-createdAtUtc,name</c> parseris (P-02).
/// <c>-</c> = mažėjančia tvarka; nežinomas laukas → 400 BAD_REQUEST.
/// Visada pridedamas stabilus tiebreaker (pvz. Id), kad puslapiai nepersidengtų.
/// </summary>
/// <example>
/// static readonly SortMap&lt;Workspace&gt; Sorts = new SortMap&lt;Workspace&gt;(w =&gt; w.Id)
///     .Add("name", w =&gt; w.Name)
///     .Add("createdAtUtc", w =&gt; w.CreatedAtUtc);
/// query = Sorts.Apply(query, sort, defaultSort: "name");
/// </example>
public sealed class SortMap<T>
{
    private delegate IOrderedQueryable<T> Orderer(IQueryable<T> query, bool descending, bool first);

    private readonly Dictionary<string, Orderer> _fields = new(StringComparer.Ordinal);
    private readonly Orderer _tiebreaker;

    public SortMap(Expression<Func<T, Guid>> tiebreaker) => _tiebreaker = Create(tiebreaker);

    public SortMap<T> Add<TKey>(string field, Expression<Func<T, TKey>> key)
    {
        _fields[field] = Create(key);
        return this;
    }

    public IQueryable<T> Apply(IQueryable<T> query, string? sort, string defaultSort)
    {
        var spec = string.IsNullOrWhiteSpace(sort) ? defaultSort : sort;
        var first = true;
        IOrderedQueryable<T>? ordered = null;

        foreach (var raw in spec.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var descending = raw.StartsWith('-');
            var field = descending ? raw[1..] : raw;
            if (!_fields.TryGetValue(field, out var orderer))
                throw ApiException.BadRequest($"Unknown sort field '{field}'. Allowed: {string.Join(", ", _fields.Keys)}.");

            ordered = orderer(ordered ?? query, descending, first);
            first = false;
        }

        return ordered is null ? _tiebreaker(query, false, true) : _tiebreaker(ordered, false, false);
    }

    private static Orderer Create<TKey>(Expression<Func<T, TKey>> key) => (query, descending, first) =>
        first
            ? descending ? query.OrderByDescending(key) : query.OrderBy(key)
            : descending ? ((IOrderedQueryable<T>)query).ThenByDescending(key) : ((IOrderedQueryable<T>)query).ThenBy(key);
}
