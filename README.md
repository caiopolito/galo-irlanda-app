# Galo Irlanda

Site da torcida de atleticanos na Irlanda: página pública, área do sócio e painel admin.

## Estrutura

```
apps/web    TanStack Start (React + Vite) — Cloudflare
apps/api    .NET 10, Minimal APIs, Vertical Slice — Fly.io
docs/       design (DESIGN.md), decisões (adr/) e layouts de referência
```

Decisões de arquitetura: [`docs/adr/`](docs/adr/README.md). Identidade visual e regras do produto: [`docs/DESIGN.md`](docs/DESIGN.md).

## Pré-requisitos

- Node 24+ e npm
- .NET SDK 10

## Comandos

```bash
npm install          # dependências do front (workspaces)
npm run dev:web      # front em http://localhost:3000
npm run dev:api      # API em http://localhost:5137 (OpenAPI em /openapi/v1.json)
npm run check:web    # Biome + TypeScript
npm run build:web
npm run test:api
```

## Convenções da API

Uma pasta por funcionalidade em `apps/api/src/GaloIrlanda.Api/Features/`. Cada arquivo de slice traz request, validação, handler e o mapeamento do endpoint (ver `Features/Health/GetHealth.cs`). Sem MediatR e sem camadas separadas — ver [ADR 0003](docs/adr/0003-backend-dotnet-vsa-enxuta.md).
