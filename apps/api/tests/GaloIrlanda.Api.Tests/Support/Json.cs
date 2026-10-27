using System.Text.Json;
using System.Text.Json.Serialization;

namespace GaloIrlanda.Api.Tests.Support;

/// <summary>Mesmas opções de JSON da API (enums em snake_case).</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
