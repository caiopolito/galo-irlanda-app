using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Common.OpenApi;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Features.Auth;
using GaloIrlanda.Api.Features.Events;
using GaloIrlanda.Api.Features.Health;
using GaloIrlanda.Api.Features.Publishing;
using GaloIrlanda.Api.Integrations.Email;

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

var app = builder.Build();

app.UseExceptionHandler();
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
groups.MapPublishingEndpoints();

await BootstrapAdmin.EnsureAsync(app.Services);

app.Run();
