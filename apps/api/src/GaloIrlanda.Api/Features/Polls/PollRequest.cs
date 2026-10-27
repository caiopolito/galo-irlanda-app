using FluentValidation;

namespace GaloIrlanda.Api.Features.Polls;

/// <summary>Campos comuns de criação e edição de votação (usados por <see cref="CreatePoll"/> e <see cref="UpdatePoll"/>).</summary>
internal static class PollRules
{
    public static void Apply<T>(AbstractValidator<T> v, Func<T, string> title, Func<T, DateTimeOffset> opens,
        Func<T, DateTimeOffset> closes, Func<T, IReadOnlyList<string>> options)
    {
        v.RuleFor(x => title(x)).NotEmpty().MaximumLength(160).OverridePropertyName("title");
        v.RuleFor(x => closes(x)).GreaterThan(x => opens(x)).OverridePropertyName("closesAt")
            .WithMessage("A votação precisa fechar depois de abrir.");
        v.RuleFor(x => options(x)).Must(o => o.Count is >= 2 and <= 10).OverridePropertyName("options")
            .WithMessage("Coloque de 2 a 10 opções.");
        v.RuleForEach(x => options(x)).NotEmpty().MaximumLength(120).OverridePropertyName("options");
    }
}
