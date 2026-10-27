# Galo Irlanda — Definições de layout

Documento de referência para continuar o desenvolvimento do site da torcida **Galo Irlanda** (atleticanos na Irlanda) em qualquer máquina.

## Link do layout

- Canvas de design (privado; compartilhar pelo menu *Share*): https://claude.ai/artifact/471n65s17UftnpCGNB5XGc
  - Página **Website · Landing A**: direção escolhida (desktop + mobile).
  - Página **App · Madrugada**: direção "C", guardada como opção para app.
  - Página **Arquivo**: propostas anteriores, só para referência.
- Cópia do HTML das telas escolhidas em [`docs/layout/`](layout/):

| Arquivo | Tela |
|---|---|
| `Landing-A.dc.html` | Landing pública, desktop (responsiva) |
| `Landing-A1.dc.html` | Área do sócio, desktop (responsiva) |
| `Landing-A-m.dc.html` | Landing pública, mobile (390 px) |
| `Landing-A1-m.dc.html` | Área do sócio, mobile (390 px) |

> Os `.dc.html` são arquivos do canvas (usam `<x-dc>` e `support.js` do artifact). Servem como referência visual e de marcação — os estilos são inline e podem ser portados direto para componentes React.

## Histórico das decisões

1. **Rodada 1** — 3 direções de app (A "Contra o Vento", B "Fanzine da Massa", C "Madrugada"). C foi a preferida, mas "faltava algo": gente, Irlanda, humor; o verde-limão lembrava fintech.
2. **Rodada 2** — 3 landings (Painel de Embarque, Flash às 3 da Manhã, Quadro de Giz). Escolhida: **Painel de Embarque**.
3. **Rodada 3** — Painel refeito para soar menos "feito por IA": fontes trocadas, botões em pílula, cantos arredondados, anotações à mão. Criada a **A1 · Área do sócio**.
4. **Rodada 4** — versões mobile da landing e da área do sócio.
5. **Rodada 5** — verde-trevo como segunda cor (variação B do agente de design), com o trevo no fim do sublinhado à mão e o painel em "feltro de sinuca".

## Conceito: "Painel de Embarque"

Cada jogo é um "voo" de volta pra BH. O site usa a linguagem de aeroporto (painel de partidas, portão, cartão de embarque, bagagem, escalas) com calor humano: fotos reais, texto mineiro e anotações escritas à mão.

## Identidade visual

### Cores

| Token | Hex | Uso |
|---|---|---|
| `bg` | `#121110` | Fundo (preto quente, não preto puro) |
| `bg-deep` | `#0B0B0A` | Painel de partidas, seção de fotos |
| `surface` | `#1C1B19` | Cards |
| `line` | `#2A2826` / `#3D3A35` | Bordas e divisores |
| `text` | `#F4F1EA` | Texto principal (off-white) e fundos claros ("papel") |
| `text-soft` | `#D3CEC4` | Parágrafos |
| `muted` | `#A8A398` | Legendas, labels |
| `amber` | `#F5A524` | Destaque de **ação e tempo**: horários, CTA, "Embarcando", anotações à mão |
| `green` | `#3A9D68` | Verde-trevo: **Irlanda e pertencimento** (trevo, "Confirmado", "Em dia", selo DUB ⇄ CNF). 5,57:1 sobre o fundo |
| `green-deep` | `#1E5C3C` | Verde sobre papel: "DUB" e selo "Sócio ativo" na carteirinha (7,02:1) |
| `green-wash` | `#17241C` | Só fundo: o "feltro de sinuca" do painel de partidas |
| `tricolor` | verde / papel / âmbar | Faixa fina no topo da carteirinha e no rodapé (o âmbar faz o papel do laranja) |
| `ink-muted` | `#6B665C` | Labels sobre fundo claro |

### Tipografia (Google Fonts)

- **Young Serif** — títulos, números grandes, monograma.
- **Figtree** (400–800) — texto e interface; `font-variant-numeric: tabular-nums` em horários.
- **Reenie Beanie** — anotações à mão, **no máximo 1 por seção** (ex.: "nem às 2 da manhã em Dublin.").

### Componentes

- **Botões**: pílula (`border-radius: 999px`), altura mínima 48–52 px. Primário âmbar com texto escuro; secundário com contorno; terciário como link sublinhado.
- **Cards**: `border-radius` 20–26 px.
- **Plaquinha do painel** (split-flap): `background: linear-gradient(#262421 0 49%, #0B0B0A 49% 51%, #1F1D1B 51%); border-radius: 8px;`
- **Status**: pílulas — âmbar cheio ("Embarcando"), contorno claro ("Confirmado"), contorno apagado ("14 de 20 vagas"), tracejado ("Em votação").
- **Toques humanos**: fotos/cartões levemente rotacionados (±1,5–3°), selo redondo "DUB ⇄ CNF · 9.000 km de saudade", cartão de embarque com picote tracejado.
- **Monograma**: "GI" em Young Serif dentro de um círculo com contorno, girado −8°.
- **Placeholders de foto**: listras diagonais `repeating-linear-gradient(135deg, #24221F 0 14px, #1E1C1A 14px 28px)` com legenda do que deve ir ali.

### Layout responsivo

- Container `max-width: 1240px`, padding lateral `clamp(16px, 4vw, 56px)`.
- Tamanhos com `clamp()`, grids `repeat(auto-fit, minmax(...))`, `flex-wrap`.
- **Mobile (≤ 480 px)**: painel de partidas vira cartões empilhados; cidades e benefícios viram carrossel horizontal; linha do tempo vertical; área do sócio ganha barra de navegação fixa embaixo (Carteirinha · Jogos · Votações · Perfil).

## Estrutura das páginas

### Landing pública (A)

1. Header — monograma, nav (Jogos, Da Torcida, Fotos, Pubs, Área do sócio), CTA "Entrar no grupo".
2. Hero — "O Galo nunca joga sozinho." + "nem às 2 da manhã em Dublin." + foto + cartão "Próximo embarque".
3. Painel de partidas — relógios BH/Dublin + tabela de jogos e encontros.
4. Portão 01 — próximo jogo (data, horário duplo, pub, volta pra casa) + "Tô dentro" / "Pôr na agenda".
5. Três portões — Dublin (DUB), Cork (ORK), Galway (GWY).
6. Da Torcida — notícias.
7. Bagagem de mão — mural de fotos.
8. Escalas — 1908, 1950, 1971, 2013, 2021.
9. Seja sócio — cartão de embarque + CTA para a área do sócio.
10. Footer — "Eu acredito. Em qualquer fuso." + aviso legal.

### Área do sócio (A1)

Saudação ("Bom dia, [Nome]." / "ou boa madrugada, né") · carteirinha em formato de cartão de embarque com QR + Wallet/PDF · próximo embarque com RSVP (Vou / Talvez / Não vou) · meus embarques · mensalidade e recibos · votação da torcida · benefícios · fotos em que o sócio aparece · documentos (estatuto, ata, regras) · indicar amigo.

## Premissas (regras do projeto)

- **Nunca usar azul** — remete ao Cruzeiro. Preto, off-white e âmbar.
- **Escudo oficial**: pode ser usado no layout ([ADR 0012](adr/0012-uso-escudo-oficial.md)), sempre num único componente. O monograma "GI" segue como marca própria da torcida.
- **Verde-trevo é a cor da Irlanda e de quem é da casa** (trevo de 3 folhas, DUB, confirmado, sócio). O âmbar continua mandando em tudo que é hora e ação. Nada de verde em botão, em horário, em bloco grande saturado ou em tom de fintech. Trevo desenhado à mão (`components/ui/Clover.tsx`), de 3 folhas; o de 4 folhas só como piada pontual.
- **Horário sempre duplo**: Dublin + Brasília, guardado em UTC e convertido por fuso, nunca por diferença fixa ([ADR 0009](adr/0009-fuso-horario-utc.md)).
- **Não inventar dados**: usar placeholders `[Adversário]`, `[Pub parceiro]`, `€[valor]`, `[X]%`, `[dd/mm]` até ter informação real. Confirmar fatos históricos (ex.: "1950 – Campeões do Gelo") em fonte oficial antes de publicar.
- **Aviso legal no rodapé**: "Torcida independente de atleticanos na Irlanda. Sem vínculo oficial com o Clube Atlético Mineiro."
- **Tom**: mineiro, bem-humorado, de gente — não corporativo.
- **Site primeiro, como PWA**: a área do sócio mobile funciona como "app instalável"; evitar manter um app nativo separado.
- **Fotos reais > ilustração**: o layout precisa funcionar com fotos de celular imperfeitas.
- **Acessibilidade**: alvos de toque ≥ 44 px, contraste alto, funcionar em 360 px de largura.
- **Editável por voluntários**: conteúdo (jogos, notícias, fotos, benefícios) gerenciado pelo painel admin em `/admin` ([ADR 0004](adr/0004-conteudo-no-backend-admin-react.md)).

## Arquitetura

Decisões registradas em [`docs/adr/`](adr/README.md). Resumo:

- Monorepo: `apps/web` (TanStack Start + React + Vite, Cloudflare), `apps/api` (.NET 10, Minimal APIs, Vertical Slice enxuta, Fly.io), `packages/api-client` (gerado do OpenAPI).
- Postgres no Neon (UE), arquivos no Cloudflare R2, login por link mágico com cookie, Stripe para mensalidade.
- O código CRA/MUI atual será removido ([ADR 0013](adr/0013-remocao-codigo-legado.md)).

## Fases

| Fase | Entrega |
|---|---|
| 0 | API + Postgres, login, papéis, admin de posts/jogos/fotos, site público consumindo a API |
| 1 | Área do sócio: perfil, carteirinha (QR + PDF), RSVP, documentos — **feito** |
| 2 | Mensalidade (Stripe Checkout + Customer Portal) — **feito** (adesão: pagou, virou sócio) |
| 3 | Votações (secretas ou abertas), benefícios, fotos com marcação — **feito**; Google/Apple Wallet pendente |

## Pendências

- Domínio (`.com`, `.ie` ou ambos) — necessário antes do login em produção.
- Definir quem detém a conta Stripe enquanto a associação for informal.
- Ensaio fotográfico real num jogo para substituir os placeholders.
