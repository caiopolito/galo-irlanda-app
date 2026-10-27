using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Posts;

public static class PostsModule
{
    public static void MapPostsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/posts").WithTags("Admin · Notícias");
        ListPostCategories.Map(admin);
        CreatePost.Map(admin);
        UpdatePost.Map(admin);
        GetPost.Map(admin);
        ListPosts.Map(admin);
        PublishPost.Map(admin);
        DeletePost.Map(admin);

        var @public = groups.Public.MapGroup("/posts").WithTags("Público");
        ListPublishedPosts.Map(@public);
        GetPublishedPost.Map(@public);
    }
}
