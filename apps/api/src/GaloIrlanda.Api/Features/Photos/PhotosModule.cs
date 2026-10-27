using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Photos;

public static class PhotosModule
{
    public static IServiceCollection AddPhotos(this IServiceCollection services) =>
        services.AddScoped<PhotoProcessor>();

    public static void MapPhotosEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/photos").WithTags("Admin · Fotos");
        UploadPhoto.Map(admin);
        UpdatePhoto.Map(admin);
        GetPhoto.Map(admin);
        ListPhotos.Map(admin);
        DeletePhoto.Map(admin);
    }
}
