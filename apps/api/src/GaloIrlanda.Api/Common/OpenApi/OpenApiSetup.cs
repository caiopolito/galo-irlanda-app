using Microsoft.AspNetCore.OpenApi;

namespace GaloIrlanda.Api.Common.OpenApi;

public static class OpenApiSetup
{
    public static IServiceCollection AddGaloOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options => options.CreateSchemaReferenceId = info =>
            info.Type.IsNested || (info.Type.IsGenericType && info.Type.Assembly == typeof(OpenApiSetup).Assembly)
                ? SchemaName(info.Type)
                : OpenApiOptions.CreateDefaultSchemaReferenceId(info));

    /// <summary>
    /// Nomes únicos no client TypeScript: <c>CreateEvent.Request</c> → <c>CreateEventRequest</c>;
    /// <c>PagedResponse&lt;ListEvents.Item&gt;</c> → <c>PagedResponseOfListEventsItem</c>.
    /// </summary>
    private static string SchemaName(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return name + "Of" + string.Concat(type.GetGenericArguments().Select(SchemaName));
        }

        return type.IsNested ? type.DeclaringType!.Name + type.Name : type.Name;
    }
}
