# Architecture Decision Records

Decisões de arquitetura do Galo Irlanda. Cada ADR registra o contexto, a decisão e as consequências. Para mudar uma decisão, crie um ADR novo que substitua o anterior (não reescreva o histórico).

| # | Decisão | Status |
|---|---|---|
| [0001](0001-monorepo.md) | Monorepo com `apps/web`, `apps/api` | Aceito |
| [0002](0002-frontend-tanstack-start.md) | Frontend em TanStack Start (React + Vite) | Aceito |
| [0003](0003-backend-dotnet-vsa-enxuta.md) | Backend .NET 10 com Vertical Slice enxuta | Aceito |
| [0004](0004-conteudo-no-backend-admin-react.md) | Conteúdo no backend, gerenciado por painel admin em React | Aceito |
| [0005](0005-dados-postgres-neon.md) | Postgres gerenciado na UE (Neon) com EF Core | Aceito |
| [0006](0006-auth-magic-link-cookie.md) | Login por link mágico + cookie de sessão no mesmo site | Aceito (Identity substituído por 0014) |
| [0007](0007-hospedagem-cloudflare-fly.md) | Front na Cloudflare, API no Fly.io | Aceito |
| [0008](0008-pagamentos-stripe.md) | Stripe como fonte da verdade da mensalidade | Aceito |
| [0009](0009-fuso-horario-utc.md) | Horários em UTC, exibidos em Dublin e Brasília | Aceito |
| [0010](0010-fotos-r2-consentimento.md) | Fotos no R2, com consentimento e sem biometria | Aceito (upload substituído por 0016) |
| [0011](0011-carteirinha-wallet.md) | Carteirinha com QR assinado; Wallet como extra | Aceito |
| [0012](0012-uso-escudo-oficial.md) | Uso do escudo oficial do Atlético no layout | Aceito |
| [0013](0013-remocao-codigo-legado.md) | Remoção do código CRA/MUI existente | Aceito |
| [0014](0014-auth-tabelas-proprias.md) | Login com tabelas próprias, sem ASP.NET Identity | Aceito |
| [0015](0015-convencoes-modelo-de-dados.md) | Convenções do modelo de dados | Aceito |
| [0016](0016-upload-de-fotos-pela-api.md) | Upload de fotos pela API, processadas com SkiaSharp | Aceito |

## Modelo

```markdown
# NNNN. Título

- Status: Proposto | Aceito | Substituído por NNNN
- Data: AAAA-MM-DD

## Contexto
## Decisão
## Consequências
```
