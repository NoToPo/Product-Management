using MediatR;
using ProductManagement.Api.Common;
using ProductManagement.Api.Contracts;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.AddVariant;
using ProductManagement.Application.Products.CreateProduct;
using ProductManagement.Application.Products.DeleteProduct;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.GetProductById;
using ProductManagement.Application.Products.ListProducts;
using ProductManagement.Application.Products.UpdateProduct;
using ProductManagement.Domain.Enums;

namespace ProductManagement.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapPost("/", async (CreateProductRequest request, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            var command = new CreateProductCommand(request.Name, request.CategoryId, request.Description, request.Brand, request.Attributes);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(product =>
            {
                response.Headers.ETag = product.ETag;
                return Results.Created($"/api/v1/products/{product.Id}", product);
            });
        })
        .WithSummary("Create a product")
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id), ct);
            return result.ToHttpResult(product =>
            {
                response.Headers.ETag = product.ETag;
                return Results.Ok(product);
            });
        })
        .WithSummary("Get a product by id")
        .Produces<ProductResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (
            ISender sender,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            Guid? categoryId = null,
            ProductStatus? status = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null) =>
        {
            var query = new ListProductsQuery(page, pageSize, search, categoryId, status, minPrice, maxPrice, sort);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithSummary("List products (paged, filterable, sortable)")
        .Produces<PagedResult<ProductResponse>>()
        .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, HttpRequest httpRequest, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            if (!ETag.TryParse(httpRequest.Headers.IfMatch, out var version))
                return Results.Problem(detail: "An 'If-Match' header with the current ETag is required.", statusCode: StatusCodes.Status428PreconditionRequired, title: "PreconditionRequired");

            var command = new UpdateProductCommand(id, version, request.Name, request.CategoryId, request.Description, request.Brand, request.Attributes);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(product =>
            {
                response.Headers.ETag = product.ETag;
                return Results.Ok(product);
            });
        })
        .WithSummary("Update a product (requires If-Match for optimistic concurrency)")
        .Produces<ProductResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id), ct);
            return result.ToHttpResult(() => Results.NoContent());
        })
        .WithSummary("Soft-delete a product")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/variants", async (Guid id, AddVariantRequest request, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            var command = new AddVariantCommand(id, request.Sku, request.Price, request.Currency, request.StockQuantity, request.Attributes);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(variant =>
            {
                response.Headers.ETag = variant.ETag;
                return Results.Created($"/api/v1/variants/{variant.Id}", variant);
            });
        })
        .WithSummary("Add a variant (SKU) to a product")
        .Produces<VariantResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
