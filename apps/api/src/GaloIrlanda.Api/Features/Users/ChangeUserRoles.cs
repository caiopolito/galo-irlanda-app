using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Users;

/// <summary>Define os papéis de um usuário. Vale na hora para as sessões abertas. Sempre sobra ao menos um admin.</summary>
public static class ChangeUserRoles
{
    private static readonly string[] KnownRoles = [Roles.Admin, Roles.Editor, Roles.Socio];

    public sealed record Request(IReadOnlyList<string> Roles);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() =>
            RuleForEach(x => x.Roles).Must(r => KnownRoles.Contains(r)).WithMessage("Papel desconhecido.");
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}/roles", Handle).WithName(nameof(ChangeUserRoles)).WithSummary("Define os papéis de um usuário")
            .WithValidation<Request>();

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, AppDbContext db, CurrentUser currentUser, TimeProvider clock, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id && u.Status != UserStatus.Erased, ct);
        if (user is null) return TypedResults.NotFound();

        var wanted = request.Roles.Distinct().ToHashSet();
        var losesAdmin = user.Roles.Any(r => r.Role == Roles.Admin) && !wanted.Contains(Roles.Admin);
        if (losesAdmin && !await db.UserRoles.AnyAsync(r => r.Role == Roles.Admin && r.UserId != id, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["roles"] = ["O site precisa de pelo menos um admin."] });

        var now = clock.GetUtcNow();
        foreach (var role in user.Roles.Where(r => !wanted.Contains(r.Role)).ToList())
        {
            user.Roles.Remove(role);
            UserAudit.Add(db, currentUser.Id, id, "role_revoke", now, new { role = role.Role });
        }
        foreach (var role in wanted.Where(w => user.Roles.All(r => r.Role != w)))
        {
            user.Roles.Add(new UserRole { UserId = id, Role = role, GrantedAt = now, GrantedBy = currentUser.Id });
            UserAudit.Add(db, currentUser.Id, id, "role_grant", now, new { role });
        }
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
