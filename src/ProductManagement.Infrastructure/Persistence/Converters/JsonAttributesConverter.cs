using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProductManagement.Infrastructure.Persistence.Converters;

/// <summary>
/// Serialises the schema-less attribute dictionary to/from JSON text, stored in a
/// PostgreSQL <c>jsonb</c> column. Keeping it as <c>jsonb</c> (not <c>text</c>) lets
/// the database index and query the contents via a GIN index.
/// </summary>
public sealed class JsonAttributesConverter : ValueConverter<Dictionary<string, object?>, string>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public JsonAttributesConverter()
        : base(
            dictionary => JsonSerializer.Serialize(dictionary, Options),
            json => JsonSerializer.Deserialize<Dictionary<string, object?>>(json, Options) ?? new Dictionary<string, object?>())
    {
    }
}
