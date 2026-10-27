# 0006. Login por link mágico + cookie de sessão no mesmo site

- Status: Aceito — o uso do ASP.NET Identity foi substituído por [0014](0014-auth-tabelas-proprias.md)
- Data: 2026-10-27

## Contexto
Sócios e voluntários não devem precisar de senha. O front fica na Cloudflare e a API no Fly; cookies entre domínios diferentes são bloqueados pelo Safari.

## Decisão
- **Link mágico por e-mail**: token aleatório, guardado só como hash, uso único, validade de 15 min, rate limit.
- **Passkeys** como melhoria posterior (suporte nativo do ASP.NET Identity no .NET 10). Google opcional.
- Sessão em cookie `HttpOnly; Secure; SameSite=Lax`. Nada de token em `localStorage`.
- Front e API no **mesmo domínio raiz** (`<dominio>` e `api.<dominio>`), nunca `*.fly.dev` em produção.
- CORS restrito às origens do site; header customizado obrigatório em mutações (anti-CSRF).
- E-mail transacional (Brevo ou Resend, região UE) com SPF, DKIM e DMARC desde o primeiro dia.

## Consequências
- O domínio precisa estar comprado antes do primeiro deploy de produção com login.
- ASP.NET Identity "core" (sem UI) cuida de usuários e papéis.
