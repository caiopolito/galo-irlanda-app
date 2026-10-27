using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Documents;

public static class DocumentsModule
{
    public static void MapDocumentsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/documents").WithTags("Admin · Documentos");
        UploadDocument.Map(admin);
        ManageDocuments.Map(admin);
    }
}
