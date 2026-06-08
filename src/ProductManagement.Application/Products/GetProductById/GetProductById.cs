using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.Mapping;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductResponse>>;

public sealed class GetProductByIdHandler(IAppDbContext db, IProductCache cache)
    : IRequestHandler<GetProductByIdQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        // Cache-aside: serve from Redis if present.
        var cached = await cache.GetAsync(request.Id, ct);
        if (cached is not null)
            return cached;

        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Error.NotFound("Product.NotFound", $"Product '{request.Id}' was not found.");

        var response = ProductMapper.ToResponse(product);
        await cache.SetAsync(response, ct);
        return response;
    }
}
