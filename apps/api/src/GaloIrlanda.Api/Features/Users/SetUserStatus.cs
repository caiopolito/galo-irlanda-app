using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Users;

/// <summary>Desativa ou reativa um usuário. Desativar derruba as sessões dele na hora.</summary>
public static class SetUserStatus
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/disable", (Guid id, AppDbContext db, CurrentUser user, TimeProvider clock, CancellationToken ct) =>
                Handle(id, UserStatus.Disabled, db, user, clock, ct))
            .WithName("DisableUser").WithSummary("Desativa um usuário");
        group.MapPost("/{id:guid}/enable", (Guid id, AppDbContext db, CurrentUser user, TimeProvider clock, CancellationToken ct) =>
                Handle(id, UserStatus.Active, db, user, clock, ct))
            .WithName("EnableUser").WithSummary("Reativa um usuário");
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id, UserStatus status, AppDbContext db, CurrentUser currentUser, TimeProvider clock, CancellationToken ct)
    {
        if (id == currentUser.Id && status == UserStatus.Disabled)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["id"] = ["Você não pode desativar a própria conta."] });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status != UserStatus.Erased, ct);
        if (user is null) return TypedResults.NotFound();

        user.Status = status;
        UserAudit.Add(db, currentUser.Id, id, status == UserStatus.Disabled ? "disable" : "enable", clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
