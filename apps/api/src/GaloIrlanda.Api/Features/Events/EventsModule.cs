using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Events;

public static class EventsModule
{
    public static void MapEventsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/events").WithTags("Admin · Eventos");
        CreateEvent.Map(admin);
        UpdateEvent.Map(admin);
        GetEvent.Map(admin);
        ListEvents.Map(admin);
        PublishEvent.Map(admin);
        DeleteEvent.Map(admin);

        ListUpcomingEvents.Map(groups.Public.MapGroup("/events").WithTags("Público"));
    }
}
