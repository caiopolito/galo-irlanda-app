# 0013. Remoção do código CRA/MUI existente

- Status: Aceito
- Data: 2026-10-27

## Contexto
O repositório tem um app Create React App + MUI com o layout antigo, publicado via `gh-pages` com hash router.

## Decisão
Com a arquitetura definida, o código existente será removido por completo e o projeto recomeça na estrutura de [0001](0001-monorepo.md). Os assets que forem reaproveitados (fotos, escudo) são movidos para `apps/web`.

## Consequências
- O deploy via `gh-pages` é aposentado; rotas antigas `/#/...` redirecionam para as novas.
