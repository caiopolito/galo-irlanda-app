# Galo Irlanda

Site da torcida de atleticanos na Irlanda: página pública, área do sócio e painel admin.

## Estrutura

```
apps/web             TanStack Start (React + Vite) — site, área do sócio e admin (/admin) — Cloudflare
apps/api             .NET 10, Minimal APIs, Vertical Slice — Fly.io
packages/api-client  client TypeScript gerado do OpenAPI da API
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

### Conteúdo inicial

Cadastra no banco o que está no layout da landing (as 6 fotos reais, foto da home, eventos, notícias, locais e marcos). Roda uma vez; não faz nada se já houver fotos ou eventos:

```bash
npm run db:seed-layout
```

Em desenvolvimento as fotos ficam em `apps/api/src/GaloIrlanda.Api/.data/media` e são servidas em `http://localhost:5137/media/`.

### Admin

Com a API e o front rodando (`npm run db:up`, `npm run dev:api`, `npm run dev:web`), acesse http://localhost:3000/entrar. Em desenvolvimento o front repassa `/api` para a API (proxy do Vite), então tudo roda no mesmo endereço.

Mudou a API? Regenere os tipos do front com a API rodando:

```bash
npm run api:types
```

### Login local

Em desenvolvimento o e-mail não é enviado: o link de acesso aparece no log da API. Para criar o primeiro admin:

```bash
Admin__BootstrapEmail=voce@exemplo.com npm run dev:api
# depois: POST /api/auth/magic-link {"email": "voce@exemplo.com"} e use o token do log em /api/auth/magic-link/consume
```

### Área do sócio e Stripe

"Pagou, virou sócio": `/seja-socio` manda para o Stripe Checkout e o webhook `POST /stripe/webhook` cria o sócio quando o pagamento é confirmado. Para testar com o Stripe em modo de teste:

1. Crie um produto com preço mensal recorrente no painel do Stripe (modo teste) e copie o `price_...`.
2. Configure `Stripe__SecretKey` (`sk_test_...`), `Stripe__PriceId` e `Stripe__WebhookSecret`.
3. Localmente, encaminhe os eventos com a Stripe CLI: `stripe listen --forward-to localhost:5137/stripe/webhook` (ela mostra o `whsec_...`).

Sem o Stripe configurado, o checkout responde 503. Para testar a área do sócio assim mesmo, dá para tornar um usuário local sócio direto no banco (papel `socio` + linhas em `members` e `subscriptions`).

Configuração em produção (segredos do Fly): `ConnectionStrings__Db`, `ADMIN_BOOTSTRAP_EMAIL`, `Email__ResendApiKey`, `Email__From`, `Site__PublicUrl`, `Site__Origins__0`, `Cloudflare__DeployHookUrl`, `Internal__Secret`, `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__PriceId`, `Storage__*` (R2).

Nova migration:

```bash
cd apps/api && dotnet ef migrations add NomeDaMudanca --project src/GaloIrlanda.Api --output-dir Data/Migrations
```
