# 0003. Backend .NET 10 com Vertical Slice enxuta

- Status: Aceito
- Data: 2026-10-27

## Contexto
O dono do projeto escolheu .NET e vai mantê-lo. O backend nasce já na primeira fase, para servir o conteúdo do site e o painel admin (ver [0004](0004-conteudo-no-backend-admin-react.md)). O domínio é pequeno; camadas e cerimônia pesariam mais que o problema.

## Decisão
- **Um projeto** `GaloIrlanda.Api` (+ projeto de testes). Sem Domain/Application/Infrastructure separados.
- **Minimal APIs**, uma pasta por funcionalidade: `Features/Posts/CreatePost.cs`, `Features/Matches/Rsvp.cs`... Cada arquivo traz request, validator, handler e o mapeamento do endpoint. Registro explícito via `MapGroup`, sem reflexão.
- **Sem MediatR** (licença comercial e sem ganho aqui). Comportamentos transversais via endpoint filters.
- FluentValidation, `ProblemDetails`, retornos `TypedResults`.
- OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`) gera o client TypeScript em `packages/api-client`; o CI quebra se o contrato divergir.
- Testes de integração por slice: xUnit + `WebApplicationFactory` + Testcontainers (Postgres).
- Sem Native AOT (EF Core); imagem chiseled + ReadyToRun.
- Tarefas agendadas (lembretes, cobranças, rebuild): endpoint `/internal/*` protegido por segredo, chamado por cron (GitHub Actions ou Cloudflare). Sem Hangfire, porque a máquina suspende quando ociosa.

## Consequências
- Duas linguagens no repositório (C# e TypeScript); o contrato OpenAPI é a ponte.
- Proibido por padrão: repositório genérico sobre EF, CQRS com dois bancos, mensageria, microserviços.
