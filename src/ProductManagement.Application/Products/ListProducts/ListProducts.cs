using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Application.Products.ListProducts;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    ProductStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null) : IRequest<Result<PagedResult<ProductResponse>>>;

public sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public const int MaxPageSize = 100;

    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be greater than or equal to MinPrice.");
    }
}

public sealed class ListProductsHandler(IAppDbContext db)
    : IRequestHandler<ListProductsQuery, Result<PagedResult<ProductResponse>>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(ListProductsQuery request, CancellationToken ct)
    {
        // Read path: no tracking, soft-deleted excluded by global query filter.
        var query = db.Products.AsNoTracking().Include(p => p.Variants).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Provider-agnostic case-insensitive contains (EF translates to LOWER(..) LIKE).
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Brand != null && p.Brand.ToLower().Contains(term)));
        }

        if (request.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);

        if (request.Status is { } status)
            query = query.Where(p => p.Status == status);

        if (request.MinPrice is { } min)
            query = query.Where(p => p.Variants.Any(v => v.Price.Amount >= min));

        if (request.MaxPrice is { } max)
            query = query.Where(p => p.Variants.Any(v => v.Price.Amount <= max));

        query = ApplySort(query, request.Sort);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var response = new PagedResult<ProductResponse>(
            items.Select(ProductMapper.ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total);

        return response;
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort) => sort?.ToLowerInvariant() switch
    {
        "name" => query.OrderBy(p => p.Name),
        "-name" => query.OrderByDescending(p => p.Name),
        "createdat" => query.OrderBy(p => p.CreatedAt),
        "-createdat" => query.OrderByDescending(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.CreatedAt) // newest first by default
    };
}
