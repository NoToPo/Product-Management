using MediatR;
using ProductManagement.Api.Common;
using ProductManagement.Api.Contracts;
using ProductManagement.Application.Categories.CreateCategory;
using ProductManagement.Application.Categories.Dtos;
using ProductManagement.Application.Categories.ListCategories;

namespace ProductManagement.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories");

        group.MapPost("/", async (CreateCategoryRequest request, ISender sender, HttpResponse response, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateCategoryCommand(request.Name, request.ParentId), ct);
            return result.ToHttpResult(category =>
            {
                response.Headers.ETag = category.ETag;
                return Results.Created($"/api/v1/categories/{category.Id}", category);
            });
        })
        .WithSummary("Create a category")
        .Produces<CategoryResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListCategoriesQuery(), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithSummary("List all categories")
        .Produces<IReadOnlyList<CategoryResponse>>();

        return app;
    }
}
