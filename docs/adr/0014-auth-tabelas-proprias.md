# 0014. Login com tabelas próprias, sem ASP.NET Identity

- Status: Aceito
- Data: 2026-10-27
- Substitui a consequência "ASP.NET Identity core cuida de usuários e papéis" do [0006](0006-auth-magic-link-cookie.md). O restante do 0006 continua valendo.

## Contexto
O login é só por link mágico (sem senha). O ASP.NET Identity traz 7 tabelas e conceitos pensados para senha (hash, lockout, claims, logins externos), quase todos sem uso aqui.

## Decisão
- Tabelas próprias: `users`, `user_roles` (`admin`, `editor`, `socio`), `login_tokens`, `sessions`.
- **Link mágico**: 32 bytes aleatórios; só o SHA-256 vai para o banco; validade de 15 min; consumo atômico (`UPDATE ... WHERE consumed_at IS NULL AND expires_at > now()`). O link aponta para o front (`/entrar?token=...`), que chama `POST /api/auth/magic-link/consume` — nunca consumir em GET (scanners de e-mail abrem links).
- **Sessão**: cookie auth do ASP.NET Core com `ITicketStore` próprio sobre `sessions` (id opaco no cookie, hash no banco). Permite "sair de todos os dispositivos" e bloquear um voluntário na hora.
- Rate limit por IP (`RateLimiter`) e por e-mail (máx. 3 tokens em 15 min). Resposta idêntica para e-mail desconhecido.
- Chaves do Data Protection persistidas no banco (`data_protection_keys`); sem isso cada restart no Fly desloga todo mundo.
- Primeiro admin criado no boot a partir de `ADMIN_BOOTSTRAP_EMAIL` (segredo do Fly), se não houver nenhum admin.
- Passkeys no futuro: tabela `passkeys` + biblioteca Fido2, sem adotar o Identity.

## Consequências
- ~200 linhas de código de autenticação nossas, cobertas por testes de integração.
- Sem as proteções prontas do Identity para senha — que não usamos.
