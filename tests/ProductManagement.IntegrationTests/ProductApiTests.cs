using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace ProductManagement.IntegrationTests;

public class ProductApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Seeded_catalogue_is_listed()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        body.GetProperty("totalCount").GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_product_then_get_returns_it_with_etag()
    {
        var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Shoes");

        var create = await client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Running Shoe",
            categoryId,
            description = "Lightweight runner",
            brand = "Northwind",
            attributes = new Dictionary<string, object> { ["material"] = "Mesh" }
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await create.Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = product.GetProperty("id").GetGuid();

        var get = await client.GetAsync($"/api/v1/products/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        get.Headers.ETag.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_with_stale_etag_returns_conflict()
    {
        var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Hats");
        var (id, etag) = await CreateProductAsync(client, "Wool Beanie", categoryId);

        // First update with the correct ETag succeeds.
        var first = await SendUpdate(client, id, etag, categoryId, "Wool Beanie v2");
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reusing the now-stale ETag must conflict.
        var second = await SendUpdate(client, id, etag, categoryId, "Wool Beanie v3");
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_reservations_never_oversell()
    {
        var client = factory.CreateClient();
        var categoryId = await CreateCategoryAsync(client, "Tees");
        var (productId, _) = await CreateProductAsync(client, "Concurrency Tee", categoryId);

        const int stock = 10;
        const int attempts = 20;
        var sku = $"CONC-{Guid.NewGuid():N}";

        var addVariant = await client.PostAsJsonAsync($"/api/v1/products/{productId}/variants", new
        {
            sku,
            price = 9.99m,
            currency = "USD",
            stockQuantity = stock,
            attributes = new Dictionary<string, object> { ["size"] = "M" }
        });
        addVariant.StatusCode.Should().Be(HttpStatusCode.Created);
        var variantId = (await addVariant.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        // Fire many single-unit reservations simultaneously.
        var tasks = Enumerable.Range(0, attempts).Select(_ =>
            client.PostAsJsonAsync($"/api/v1/variants/{variantId}/reserve", new { quantity = 1 }));
        var responses = await Task.WhenAll(tasks);

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflicted = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        // Exactly `stock` succeed; the rest are rejected — no oversell.
        succeeded.Should().Be(stock);
        conflicted.Should().Be(attempts - stock);

        var product = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products/{productId}", Json);
        var variant = product.GetProperty("variants").EnumerateArray()
            .First(v => v.GetProperty("id").GetGuid() == variantId);
        variant.GetProperty("reservedQuantity").GetInt32().Should().Be(stock);
        variant.GetProperty("availableQuantity").GetInt32().Should().Be(0);
    }

    // --- helpers ---

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
    }

    private static async Task<(Guid Id, string ETag)> CreateProductAsync(HttpClient client, string name, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/products", new { name, categoryId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
        var etag = response.Headers.ETag!.Tag;
        return (id, etag);
    }

    private static Task<HttpResponseMessage> SendUpdate(HttpClient client, Guid id, string etag, Guid categoryId, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/products/{id}")
        {
            Content = JsonContent.Create(new { name, categoryId })
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return client.SendAsync(request);
    }
}
