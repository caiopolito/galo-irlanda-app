# 0009. Horários em UTC, exibidos em Dublin e Brasília

- Status: Aceito
- Data: 2026-10-27

## Decisão
- Banco e API guardam e trafegam instantes em UTC (ISO 8601).
- A interface exibe sempre os dois fusos: `Europe/Dublin` e `America/Sao_Paulo`, via `Intl.DateTimeFormat`. Nunca somar diferença fixa.
- Indicar "(dia anterior)" quando a data muda entre os fusos.
- "Pôr na agenda": arquivo `.ics` + link do Google Calendar.
- Testes cobrindo as viradas de horário de verão da Irlanda.
