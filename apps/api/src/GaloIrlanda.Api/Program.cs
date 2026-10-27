using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Common.OpenApi;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Features.Auth;
using GaloIrlanda.Api.Features.Cities;
using GaloIrlanda.Api.Features.Events;
using GaloIrlanda.Api.Features.Health;
using GaloIrlanda.Api.Features.History;
using GaloIrlanda.Api.Features.Landing;
using GaloIrlanda.Api.Features.Milestones;
using GaloIrlanda.Api.Features.Photos;
using GaloIrlanda.Api.Features.Posts;
using GaloIrlanda.Api.Features.Publishing;
using GaloIrlanda.Api.Features.Seed;
using GaloIrlanda.Api.Features.Settings;
using GaloIrlanda.Api.Features.Users;
using GaloIrlanda.Api.Features.Venues;
using GaloIrlanda.Api.Integrations.Email;
using GaloIrlanda.Api.Integrations.Storage;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddProblemDetails();
services.AddExceptionHandler<DbExceptionHandler>();
services.AddGaloOpenApi();
services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
services.AddSingleton(TimeProvider.System);
services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
services.AddGaloData(builder.Configuration);
services.AddGaloAuth();
services.AddAuthFeature(builder.Configuration);
services.AddEmail(builder.Configuration, builder.Environment);
services.AddGaloCors(builder.Configuration);
services.AddPublishing();
services.AddStorage(builder.Configuration);
services.AddPhotos();

var app = builder.Build();

// dotnet run -- seed-layout [pasta-das-fotos]: cadastra o conteúdo do layout e sai.
if (args is [SeedLayoutContent.Command, ..])
{
    var photosPath = args.Length > 1 ? args[1] : Path.Combine(app.Environment.ContentRootPath, "../../../web/src/assets/photos");
    Console.WriteLine(await SeedLayoutContent.RunAsync(app.Services, Path.GetFullPath(photosPath)));
    return;
}

app.UseExceptionHandler();
app.UseLocalMedia();
app.UseStatusCodePages();
app.UseCors(CorsSetup.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

GetHealth.Map(app);

var groups = app.MapEndpointGroups();
groups.MapAuthEndpoints();
groups.MapEventsEndpoints();
groups.MapPostsEndpoints();
groups.MapPhotosEndpoints();
groups.MapCitiesEndpoints();
groups.MapVenuesEndpoints();
groups.MapMilestonesEndpoints();
groups.MapSettingsEndpoints();
groups.MapUsersEndpoints();
groups.MapHistoryEndpoints();
groups.MapPublishingEndpoints();
groups.MapLandingEndpoints();

await BootstrapAdmin.EnsureAsync(app.Services);

app.Run();
