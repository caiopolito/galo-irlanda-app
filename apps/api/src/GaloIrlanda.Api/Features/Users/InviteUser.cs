using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Email;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Users;

/// <summary>
/// Convida um voluntário: cria (ou reaproveita) o usuário com o papel e manda um e-mail com o endereço
/// de entrada. O acesso em si é sempre pelo link mágico.
/// </summary>
public static class InviteUser
{
    public static readonly string[] InvitableRoles = [Roles.Admin, Roles.Editor];

    public sealed record Request(string Email, string DisplayName, string Role);

    public sealed record Response(Guid Id);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
            RuleFor(x => x.Role).Must(r => InvitableRoles.Contains(r)).WithMessage("Papel deve ser admin ou editor.");
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/invite", Handle).WithName(nameof(InviteUser)).WithSummary("Convida um voluntário")
            .WithValidation<Request>();

    private static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CurrentUser currentUser, IEmailSender email, IConfiguration configuration,
        TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == request.Email.Trim(), ct);
        if (user is { Status: UserStatus.Erased })
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Esta conta foi excluída."] });

        if (user is null)
        {
            user = new User { Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim() };
            db.Users.Add(user);
            UserAudit.Add(db, currentUser.Id, user.Id, "invite", now);
        }

        if (user.Roles.All(r => r.Role != request.Role))
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = request.Role, GrantedAt = now, GrantedBy = currentUser.Id });
            UserAudit.Add(db, currentUser.Id, user.Id, "role_grant", now, new { role = request.Role });
        }
        await db.SaveChangesAsync(ct);

        var link = $"{configuration["Site:PublicUrl"]}/entrar";
        await email.SendAsync(new EmailMessage(
            To: user.Email,
            Subject: "Você foi convidado para o admin do Galo Irlanda",
            Html: $"<p>Fala, {System.Net.WebUtility.HtmlEncode(user.DisplayName)}!</p><p>Você agora ajuda a cuidar do site do Galo Irlanda. Para entrar, acesse <a href=\"{link}\">{link}</a> e peça seu link de acesso com este e-mail.</p>",
            Text: $"Fala, {user.DisplayName}!\n\nVocê agora ajuda a cuidar do site do Galo Irlanda. Para entrar, acesse {link} e peça seu link de acesso com este e-mail."),
            ct);

        return TypedResults.Ok(new Response(user.Id));
    }
}
