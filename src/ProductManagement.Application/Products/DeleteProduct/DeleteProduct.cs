using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Application.Abstractions;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Products.DeleteProduct;

/// <summary>Soft-deletes a product so existing order/history references remain valid.</summary>
public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteProductHandler(IAppDbContext db, IProductCache cache, ICurrentUser user)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (product is null)
            return Error.NotFound("Product.NotFound", $"Product '{request.Id}' was not found.");

        product.SoftDelete(user.UserId);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(product.Id, ct);

        return Result.Success();
    }
}
