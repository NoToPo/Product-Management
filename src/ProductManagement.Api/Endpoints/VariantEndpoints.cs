using MediatR;
using ProductManagement.Api.Common;
using ProductManagement.Api.Contracts;
using ProductManagement.Application.Common;
using ProductManagement.Application.Products.Dtos;
using ProductManagement.Application.Products.ReleaseStock;
using ProductManagement.Application.Products.ReserveStock;
using ProductManagement.Application.Products.UpdateVariant;

namespace ProductManagement.Api.Endpoints;

public static class VariantEndpoints
{
    public static IEndpointRouteBuilder MapVariantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/variants").WithTags("Variants");

        group.MapPut("/{id:guid}", async (Guid id, UpdateVariantRequest request, HttpRequest httpRequest, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            if (!ETag.TryParse(httpRequest.Headers.IfMatch, out var version))
                return Results.Problem(detail: "An 'If-Match' header with the current ETag is required.", statusCode: StatusCodes.Status428PreconditionRequired, title: "PreconditionRequired");

            var command = new UpdateVariantCommand(id, version, request.Price, request.Currency, request.StockQuantity);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(variant =>
            {
                response.Headers.ETag = variant.ETag;
                return Results.Ok(variant);
            });
        })
        .WithSummary("Update a variant's price/stock (requires If-Match)")
        .Produces<VariantResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        group.MapPost("/{id:guid}/reserve", async (Guid id, StockQuantityRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReserveStockCommand(id, request.Quantity), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithSummary("Reserve stock (atomic, oversell-safe)")
        .Produces<VariantResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/release", async (Guid id, StockQuantityRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReleaseStockCommand(id, request.Quantity), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithSummary("Release a previously held reservation")
        .Produces<VariantResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
