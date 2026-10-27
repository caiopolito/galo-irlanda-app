# 0005. Postgres gerenciado na UE (Neon) com EF Core

- Status: Aceito
- Data: 2026-10-27

## Contexto
Volume pequeno (dezenas a poucas centenas de sócios), orçamento baixíssimo, dados pessoais de residentes na UE.

## Decisão
- Postgres no **Neon**, região UE (Londres ou Frankfurt), plano gratuito para começar.
- EF Core 10 + Npgsql. Migrations aplicadas via `dotnet ef migrations bundle` no `release_command` do Fly, nunca no startup.
- Backup: `pg_dump` diário via GitHub Actions para o R2, retenção de 30 dias, teste de restore trimestral.

## Consequências
- Sem Postgres autogerenciado no Fly (backup ficaria conosco).
- Branches do Neon podem servir de banco de preview por PR.
