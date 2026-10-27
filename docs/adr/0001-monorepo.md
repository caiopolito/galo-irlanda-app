# 0001. Monorepo com `apps/web` e `apps/api`

- Status: Aceito
- Data: 2026-10-27

## Contexto
Projeto de comunidade mantido por 1–2 devs no tempo livre. O próximo voluntário precisa achar tudo num lugar só.

## Decisão
Um único repositório:

```
apps/web        frontend (site público, área do sócio, admin)
apps/api        backend .NET
packages/api-client   client TypeScript gerado a partir do OpenAPI da API
docs/           design, ADRs, layouts de referência
```

Sem Nx/Turbo. Scripts simples (npm workspaces + `dotnet`) e GitHub Actions com filtros por caminho.

## Consequências
- Mudanças de contrato front/back acontecem no mesmo PR.
- O CI precisa rodar só o que mudou (filtros `paths:` no workflow).
