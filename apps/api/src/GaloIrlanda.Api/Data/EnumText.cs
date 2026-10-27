using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Data;

/// <summary>Enums gravados como texto snake_case (<c>NoIdentifiablePeople</c> → <c>no_identifiable_people</c>), validados por CHECK.</summary>
public static class EnumText<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> Texts = Enum.GetValues<TEnum>()
        .ToDictionary(v => v, v => JsonNamingPolicy.SnakeCaseLower.ConvertName(v.ToString()));

    private static readonly Dictionary<string, TEnum> Values = Texts.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string ToText(TEnum value) => Texts[value];

    public static TEnum Parse(string text) => Values[text];

    public static string CheckSql(string column) =>
        $"{column} IN ({string.Join(", ", Texts.Values.Select(v => $"'{v}'"))})";
}

public static class EnumPropertyBuilderExtensions
{
    public static PropertyBuilder<TEnum> HasTextConversion<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion(v => EnumText<TEnum>.ToText(v), s => EnumText<TEnum>.Parse(s));
}
