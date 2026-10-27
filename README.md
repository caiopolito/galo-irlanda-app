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
- Docker (Postgres local e testes de integração com Testcontainers)

## Comandos

```bash
npm install          # dependências do front (workspaces)
npm run dev:web      # front em http://localhost:3000
npm run db:up        # Postgres local (compose.yaml)
npm run db:migrate   # aplica as migrations do EF Core
npm run dev:api      # API em http://localhost:5137 (OpenAPI em /openapi/v1.json)
npm run check:web    # Biome + TypeScript
npm run build:web
npm run test:api
```

## API

Vertical Slice Architecture: uma pasta por recurso em `apps/api/src/GaloIrlanda.Api/Features/`, um arquivo por caso de uso (ex.: `Features/Events/CreateEvent.cs`). Guia completo: [`docs/api/ARQUITETURA.md`](docs/api/ARQUITETURA.md). Modelo de dados: [`docs/database/schema.md`](docs/database/schema.md).

Nova migration:

```bash
cd apps/api && dotnet ef migrations add NomeDaMudanca --project src/GaloIrlanda.Api --output-dir Data/Migrations
```
