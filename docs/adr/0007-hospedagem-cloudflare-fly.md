# 0007. Front na Cloudflare, API no Fly.io

- Status: Aceito
- Data: 2026-10-27

## Decisão
- **Front**: Cloudflare Pages (ou Workers Static Assets), com preview por PR. DNS do domínio na Cloudflare.
- **API**: Fly.io, região `lhr`, 1 máquina `shared-cpu-1x` 512 MB, `auto_stop_machines = "suspend"`, `min_machines_running = 0`. Ambientes `staging` e `prod`.
- **Arquivos**: Cloudflare R2.
- Segredos via `fly secrets` e variáveis da Cloudflare, nunca no repositório.
- Observabilidade: logs estruturados, OpenTelemetry para Grafana Cloud (free), Sentry (free) no front e na API, uptime externo.
- Alerta de cobrança configurado no Fly (não há mais free tier).

## Consequências
- Custo estimado: €1–10/mês + taxas do Stripe.
- Dois provedores: manter documentado quem tem acesso a cada conta.
