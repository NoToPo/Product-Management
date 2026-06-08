using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ProductManagement.Infrastructure.Persistence.Converters;

/// <summary>
/// Value comparer so EF Core can detect changes to the mutable attribute dictionary
/// (compare by serialised content, snapshot by deep clone).
/// </summary>
public sealed class JsonAttributesComparer : ValueComparer<Dictionary<string, object?>>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public JsonAttributesComparer()
        : base(
            (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
            value => value == null ? 0 : JsonSerializer.Serialize(value, Options).GetHashCode(),
            value => JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(value, Options), Options)!)
    {
    }
}
