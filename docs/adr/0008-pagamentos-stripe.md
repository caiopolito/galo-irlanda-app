# 0008. Stripe como fonte da verdade da mensalidade

- Status: Aceito
- Data: 2026-10-27

## Decisão
- Stripe Checkout (assinatura em EUR) + Customer Portal (recibos, cartão, cancelamento). Nenhum dado de cartão passa pela API.
- Webhook com verificação de assinatura e tabela de eventos processados (idempotência). Guardamos só `customer_id` e status.
- Stripe em modo de teste no `staging`.

## Consequências
- A associação é **informal**: a conta Stripe fica no nome de uma pessoa física, que responde pelos impostos. Definir quem antes da fase de pagamentos; formalizar a associação é recomendado.
