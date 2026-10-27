# 0002. Frontend em TanStack Start (React + Vite)

- Status: Aceito
- Data: 2026-10-27

## Contexto
- O site público é quase todo conteúdo e é muito compartilhado no WhatsApp/Instagram. O preview desses apps não executa JavaScript, então as páginas públicas precisam sair como HTML pronto (título, descrição, imagem OG).
- A maior parte do produto é aplicação: área do sócio (`/socio`) e painel admin (`/admin`).
- Alternativas avaliadas: Vite SPA pura (preview e SEO ruins), Astro com ilhas React (ótimo para conteúdo, mas dois modelos mentais e o produto é majoritariamente app), React Router 7 em modo framework (viável), Next.js (complexidade sem retorno).

## Decisão
TanStack Start (sobre TanStack Router + Vite), em TypeScript strict:
- **Rotas públicas**: pré-renderizadas no build, buscando conteúdo da API. Ao publicar um post/jogo, a API chama um *deploy hook* da Cloudflare e o site é regenerado (~1–2 min).
- **`/socio` e `/admin`**: renderizadas só no navegador (SPA), autenticadas via cookie.
- Se a espera do rebuild incomodar, as rotas públicas passam a SSR em Cloudflare Workers com cache, sem reescrever código.

Bibliotecas de base: TanStack Query (dados), Zod (validação), CSS Modules + tokens em CSS custom properties (sem MUI), React Aria Components (acessibilidade), Fontsource (fontes self-hosted), vite-plugin-pwa (PWA), Vitest + Testing Library, Playwright + axe, Biome. Versões conferidas na criação do projeto.

## Consequências
- Uma base de código React só para público, sócio e admin.
- TanStack Start é mais novo que Astro/Next: conferir estabilidade da versão na criação do projeto; React Router 7 em modo framework é o plano B.
- Sair do MUI exige construir os componentes do design (pílulas, plaquinhas, cartões), que já estão especificados em `docs/DESIGN.md`.
