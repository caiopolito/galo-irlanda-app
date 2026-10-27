# 0015. Convenções do modelo de dados

- Status: Aceito
- Data: 2026-10-27

## Decisão
- **Chaves**: UUID v7 gerado na aplicação (`Guid.CreateVersion7()`). Números para humanos (nº da carteirinha) em coluna própria com `SEQUENCE`. Exceções: `cities.code` (`'DUB'`) como chave natural; `bigint identity` em tabelas só de inserção (`audit_log`, `site_rebuild_requests`); `smallint` em `post_categories`.
- **Enums**: `text` + `CHECK`, mapeados com `HasConversion<string>()`. Sem enum nativo do Postgres.
- **Nomes**: snake_case (`EFCore.NamingConventions`), tabelas no plural; prefixos `ix_`, `ux_`, `ck_`, `fk_`.
- **Tempo**: `timestamptz` sempre (ADR 0009); `date` só para calendário puro.
- **Concorrência otimista**: coluna de sistema `xmin` mapeada como `Version` em toda entidade editável. Conflito vira 409.
- **Auditoria**: `SaveChangesInterceptor` preenche `created_*`/`updated_*`, grava `audit_log` (só campos alterados, `[antigo, novo]`; nunca e-mail, token, user agent, votos ou RSVP) e registra `site_rebuild_requests` quando conteúdo público muda.
- **Exclusão**:
  - Conteúdo (posts, eventos, cidades, locais, marcos): exclusão definitiva; histórico no `audit_log`. Post retirado do ar vai para `archived`.
  - Fotos: `deleted_at` como fila de purga do R2; a linha some depois.
  - Usuários: nunca apagados; anonimizados (`status = 'erased'`) para manter as FKs e cumprir o art. 17.
  - Sem `deleted_at` genérico com filtro global.
- **Status x publicação**: o que é decisão humana é gravado (`events.status`, `events.is_published`, `posts.status`); o que depende do relógio é derivado (post agendado = `published` com `published_at` futuro; selo "Embarcando" calculado no front).
- **Seed**: SQL na migration com `ON CONFLICT DO NOTHING`, nunca `HasData` para dado que voluntário edita.
- **Migrations**: uma por PR, revisadas; aplicadas pelo bundle no `release_command`; mudanças que quebram em duas etapas (expandir → contrair).

Desenho completo das tabelas: [`docs/database/schema.md`](../database/schema.md).
