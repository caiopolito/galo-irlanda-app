# 0004. Conteúdo no backend, gerenciado por painel admin em React

- Status: Aceito
- Data: 2026-10-27

## Contexto
Voluntários não-técnicos precisam editar posts, jogos, fotos e benefícios. Foram considerados CMS no Git (Sveltia/Decap) e CMS headless. Como login e permissões vão existir de qualquer forma por causa dos sócios, o painel próprio reaproveita essa base.

## Decisão
- Conteúdo fica no Postgres, servido pela API .NET.
- Painel admin em `/admin` no mesmo app React.
- Papéis: `admin`, `editor`, `socio`. Voluntários entram pelo mesmo link mágico.
- Escopo inicial do admin:
  - **Posts**: rascunho / publicado / agendado, Markdown com prévia (ou TipTap), capa.
  - **Jogos**: adversário, campeonato, horário (UTC), pub, cidade, status.
  - **Fotos**: upload direto ao R2 com remoção de EXIF/GPS e registro de consentimento.
- Histórico de alterações (quem mudou o quê e quando) em todas as entidades editáveis.
- Publicar ou despublicar dispara o rebuild do site público (ver [0002](0002-frontend-tanstack-start.md)).

## Consequências
- O painel é trabalho próprio: começar mínimo e crescer com o uso.
- Backup do banco passa a proteger também o conteúdo editorial.
