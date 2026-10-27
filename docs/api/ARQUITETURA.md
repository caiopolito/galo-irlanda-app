# API — Vertical Slice Architecture

Como o código de `apps/api` é organizado. Decisões de base: [ADR 0003](../adr/0003-backend-dotnet-vsa-enxuta.md), [0014](../adr/0014-auth-tabelas-proprias.md), [0015](../adr/0015-convencoes-modelo-de-dados.md). Banco: [`docs/database/schema.md`](../database/schema.md).

## Estrutura

```
apps/api/
├── Directory.Build.props        net10.0, Nullable, warnings como erro
├── Directory.Packages.props     versões dos pacotes num lugar só
├── src/GaloIrlanda.Api/
│   ├── Program.cs
│   ├── Common/                  o que vários slices usam
│   │   ├── Auth/                Roles, Policies, CurrentUser, AuthSetup
│   │   ├── Http/                grupos de rota, filtro CSRF, filtro /internal, erros de banco, CORS
│   │   ├── Validation/          ValidationFilter (FluentValidation)
│   │   ├── Paging/              PageRequest, PagedResponse
│   │   └── Text/                Slug
│   ├── Data/                    AppDbContext, AuditingInterceptor, Migrations
│   ├── Domain/                  entidades + configuração EF no mesmo arquivo
│   ├── Integrations/            Email, Storage (R2), Cloudflare (deploy hook)
│   └── Features/                um diretório por recurso, um arquivo por caso de uso
│       ├── Events/              EventsModule, CreateEvent, UpdateEvent, ListUpcomingEvents...
│       ├── Posts/ Photos/ Cities/ Venues/ Milestones/ Settings/ Users/ Auth/
│       ├── History/             histórico de alterações
│       ├── Publishing/          regeração do site público
│       └── Landing/             GET /api/public/landing (cruza recursos)
└── tests/GaloIrlanda.Api.Tests/
    ├── Support/                 ApiFactory (Testcontainers + Respawn), TestAuthHandler, fakes
    └── Features/                espelha src/Features
```

**Por recurso, não por público.** Admin e público do mesmo recurso ficam na mesma pasta: um campo novo em Eventos mexe em `CreateEvent`, `UpdateEvent` e `ListUpcomingEvents` sem sair de `Features/Events`. Quem decide se a rota é admin ou pública é o grupo onde o `EventsModule` registra o slice.

**Entidades compartilhadas em `Domain/`.** Um `Event` é lido por Events, Landing, Photos e History; esconder a entidade dentro de uma feature só tornaria essa dependência implícita.

## Anatomia de um slice

```csharp
public static class CreateEvent
{
    public sealed record Request(...);
    public sealed record Response(...);

    public sealed class Validator : AbstractValidator<Request> { ... }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName(nameof(CreateEvent)).WithValidation<Request>();

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CancellationToken ct) { ... }
}
```

- Classe estática com `Request`, `Response`, `Validator`, `Map` e `Handle`. Dependências entram por parâmetro do `Handle`.
- `WithName(nameof(Slice))` sempre: vira o nome da operação no client TypeScript.
- Retornos com `TypedResults` e `Results<...>` explícito.

## Rotas

| Grupo | Prefixo | Regra |
|---|---|---|
| Admin | `/api/admin/{recurso}` | papel `editor` ou `admin`; exclusões exigem `admin` |
| Público | `/api/public/{recurso}` | anônimo |
| Auth | `/api/auth/*` | anônimo, com rate limit |
| Interno | `/internal/*` | header `X-Internal-Secret`, fora do OpenAPI |

Toda mutação em `/api` exige o header `X-Galo-Csrf: 1` (força o preflight do CORS; junto com `SameSite=Lax`, cobre CSRF).

## Regras do time

1. **Um arquivo por caso de uso**, nome verbo + substantivo (`CreateEvent`, `ListUpcomingEvents`). Nada de `EventService`.
2. **Leitura**: projeção direta para o DTO do próprio slice. Nunca devolver entidade; nunca reaproveitar o DTO de outro slice.
3. **Escrita**: carregar a entidade, alterar, `SaveChangesAsync`. Datas, autor, histórico e regeração do site são automáticos (interceptor). `ExecuteUpdate`/`ExecuteDelete` só em manutenção — eles pulam o interceptor.
4. **Erros**: regra de negócio → `ValidationProblem` (400). Conflito de versão ou unicidade → 409 pelo handler global.
5. **Datas**: `DateTimeOffset` em UTC; `TimeProvider` sempre, nunca `DateTime.UtcNow`.
6. **Duplicar antes de abstrair**: copie na 1ª e na 2ª vez; na 3ª, extraia — para a pasta da feature se só ela usa, para `Common/` se várias usam.
7. **Interface só em fronteira de I/O** que o teste precisa trocar (`IEmailSender`, `IDeployHook`).
8. **Todo slice novo tem teste de integração**: caminho feliz, autorização e validação.
9. **Migrations**: uma por PR, com nome descritivo (`AddEventCapacity`), revisadas no SQL.

## Pacotes

Usamos: Npgsql EF Core, EFCore.NamingConventions, FluentValidation, AWSSDK.S3 (R2), SkiaSharp (fotos, MIT), OpenAPI nativo. Testes: xUnit v3, Mvc.Testing, Testcontainers.PostgreSql, Respawn.

Evitamos: MediatR, AutoMapper, FluentAssertions 8+, ImageSharp 4 (licenças comerciais); Swashbuckle; Carter/FastEndpoints (descoberta por reflexão); Hangfire/Quartz; repositório genérico, Specification, ErrorOr/OneOf.
