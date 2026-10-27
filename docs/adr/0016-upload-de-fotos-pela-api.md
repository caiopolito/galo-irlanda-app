# 0016. Upload de fotos pela API, processadas com SkiaSharp

- Status: Aceito
- Data: 2026-10-27
- Substitui o "upload direto do navegador para o R2 com URL pré-assinada" do [0010](0010-fotos-r2-consentimento.md). O restante do 0010 continua valendo.

## Contexto
Com URL pré-assinada, a API ainda precisaria baixar o original do R2 para remover EXIF/GPS e gerar as variantes. O ganho de banda é nulo e o fluxo fica com dois passos, CORS no bucket e fotos "pendentes" para limpar.

## Decisão
- O admin envia a foto em `multipart/form-data` para `POST /api/admin/photos` (JPEG, PNG ou WebP, até 20 MB, até 40 MP).
- A API corrige a orientação, recodifica (o que descarta todos os metadados, inclusive GPS) e grava WebP em 480, 1024 e 2048 px (`photos/{id}/{largura}.webp`), sem ampliar. O original não é guardado.
- Processamento com **SkiaSharp** (MIT). O ImageSharp foi descartado pela licença comercial dividida da versão 4.
- Armazenamento atrás de `IObjectStorage`: R2 em produção, pasta local (`.data/media`, servida em `/media`) em desenvolvimento.
- A foto nasce privada; legenda, consentimento e publicação vêm em `PUT /api/admin/photos/{id}`.
- Exclusão: a foto sai do ar na hora e o cron apaga os arquivos e depois a linha.

## Consequências
- O upload passa pela máquina do Fly (512 MB bastam para fotos de celular).
- Fotos "só para sócios" (fase 3) precisarão de bucket privado e URLs assinadas.
