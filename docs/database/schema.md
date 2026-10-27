# Modelo de dados

Referência de desenho do banco (PostgreSQL no Neon). A **fonte da verdade** são as migrations do EF Core em `apps/api/src/GaloIrlanda.Api/Data/Migrations`; este documento explica o porquê. Convenções: [ADR 0015](../adr/0015-convencoes-modelo-de-dados.md). Login: [ADR 0014](../adr/0014-auth-tabelas-proprias.md).

## Relacionamentos

```
users 1─N user_roles            (admin | editor | socio)
users 1─N login_tokens          (hash, uso único, 15 min)
users 1─N sessions              (cookie opaco, hash no banco)
users 1─N audit_log.actor       (ON DELETE SET NULL)

cities (PK code 'DUB') 1─N venues
cities 1─N events
venues 1─N events               (venue_id opcional + venue_note livre)
post_categories 1─N posts
photos ── capa de events, posts, cities; foto da home em site_settings
events 1─N photos.event_id      (álbum do evento, opcional)
site_settings                   (linha única)
site_rebuild_requests           (fila de regeração do site público)
data_protection_keys            (chaves do cookie de sessão)
```

## Fase 0 — admin e site público

```sql
CREATE EXTENSION IF NOT EXISTS citext;

-- ===== Identidade =====
CREATE TABLE users (
  id             uuid PRIMARY KEY,                 -- UUID v7 gerado na aplicação
  email          citext NOT NULL,
  display_name   text   NOT NULL CHECK (char_length(display_name) BETWEEN 1 AND 80),
  status         text   NOT NULL DEFAULT 'active' CHECK (status IN ('active','disabled','erased')),
  last_login_at  timestamptz,
  erased_at      timestamptz,
  created_at     timestamptz NOT NULL,
  updated_at     timestamptz NOT NULL,
  CHECK ((status = 'erased') = (erased_at IS NOT NULL))
);
CREATE UNIQUE INDEX ux_users_email ON users (email);

CREATE TABLE user_roles (
  user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  role        text NOT NULL CHECK (role IN ('admin','editor','socio')),
  granted_at  timestamptz NOT NULL,
  granted_by  uuid REFERENCES users(id) ON DELETE SET NULL,
  PRIMARY KEY (user_id, role)
);

CREATE TABLE login_tokens (
  id           uuid PRIMARY KEY,
  user_id      uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash   bytea NOT NULL UNIQUE,              -- SHA-256 (32 bytes)
  created_at   timestamptz NOT NULL,
  expires_at   timestamptz NOT NULL,
  consumed_at  timestamptz
);
CREATE INDEX ix_login_tokens_user_created ON login_tokens (user_id, created_at DESC);

CREATE TABLE sessions (
  id            uuid PRIMARY KEY,
  user_id       uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash    bytea NOT NULL UNIQUE,
  ticket        bytea NOT NULL,                    -- ticket do cookie auth, protegido pelo Data Protection
  user_agent    text CHECK (char_length(user_agent) <= 200),
  created_at    timestamptz NOT NULL,
  last_seen_at  timestamptz NOT NULL,
  expires_at    timestamptz NOT NULL,
  revoked_at    timestamptz
);
CREATE INDEX ix_sessions_user ON sessions (user_id) WHERE revoked_at IS NULL;

CREATE TABLE data_protection_keys (
  id            int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  friendly_name text,
  xml           text
);

-- ===== Mídia =====
-- Upload: API cria a linha 'pending' e devolve URL pré-assinada do bucket privado;
-- no confirm, o servidor remove EXIF/GPS, gera WebP 480/1024/2048 em photos/{id}/{w}.webp
-- no bucket público, apaga o original e marca 'ready'.
CREATE TABLE photos (
  id                    uuid PRIMARY KEY,
  upload_status         text NOT NULL DEFAULT 'pending' CHECK (upload_status IN ('pending','ready')),
  width                 int CHECK (width > 0),
  height                int CHECK (height > 0),
  alt_text              text CHECK (char_length(alt_text) <= 300),
  title                 text CHECK (char_length(title) <= 120),
  caption               text CHECK (char_length(caption) <= 300),
  taken_on              date,
  event_id              uuid,                       -- FK adicionada depois de events
  visibility            text NOT NULL DEFAULT 'private' CHECK (visibility IN ('public','members','private')),
  consent_basis         text NOT NULL DEFAULT 'pending'
                        CHECK (consent_basis IN ('pending','no_identifiable_people','consent_recorded','withdrawn')),
  consent_note          text CHECK (char_length(consent_note) <= 500),
  consent_confirmed_by  uuid REFERENCES users(id) ON DELETE SET NULL,
  consent_confirmed_at  timestamptz,
  wall_featured         boolean NOT NULL DEFAULT false,
  wall_sort_order       int,
  deleted_at            timestamptz,                -- fila de purga no R2
  created_by            uuid REFERENCES users(id) ON DELETE SET NULL,
  updated_by            uuid REFERENCES users(id) ON DELETE SET NULL,
  created_at            timestamptz NOT NULL,
  updated_at            timestamptz NOT NULL,
  CONSTRAINT ck_photos_public_ok CHECK (
    visibility <> 'public' OR (upload_status = 'ready' AND alt_text IS NOT NULL
      AND consent_basis IN ('no_identifiable_people','consent_recorded'))),
  CONSTRAINT ck_photos_wall CHECK (NOT wall_featured OR visibility = 'public'),
  CONSTRAINT ck_photos_consent CHECK (
    consent_basis NOT IN ('consent_recorded','no_identifiable_people') OR consent_confirmed_at IS NOT NULL)
);
CREATE INDEX ix_photos_wall ON photos (wall_sort_order) WHERE wall_featured AND deleted_at IS NULL;

-- ===== Lugares =====
CREATE TABLE cities (
  code        char(3) PRIMARY KEY CHECK (code ~ '^[A-Z]{3}$'),
  name        text NOT NULL,
  blurb       text CHECK (char_length(blurb) <= 400),
  photo_id    uuid REFERENCES photos(id) ON DELETE SET NULL,
  sort_order  smallint NOT NULL DEFAULT 0,
  is_active   boolean NOT NULL DEFAULT true,
  created_by, updated_by, created_at, updated_at   -- como em photos
);

CREATE TABLE venues (
  id                uuid PRIMARY KEY,
  city_code         char(3) NOT NULL REFERENCES cities(code) ON UPDATE CASCADE,
  name              text NOT NULL CHECK (char_length(name) BETWEEN 1 AND 120),
  neighborhood      text,
  address           text,
  maps_url          text CHECK (maps_url ~ '^https://'),
  website_url       text CHECK (website_url ~ '^https://'),
  is_partner        boolean NOT NULL DEFAULT false,
  shows_with_sound  boolean,                        -- "passa o Galo com som"
  notes             text,
  is_active         boolean NOT NULL DEFAULT true,
  created_by, updated_by, created_at, updated_at,
  UNIQUE (city_code, name)
);

-- ===== Eventos =====
CREATE TABLE events (
  id              uuid PRIMARY KEY,
  kind            text NOT NULL CHECK (kind IN ('match','meetup')),
  slug            text NOT NULL UNIQUE CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
  title           text NOT NULL CHECK (char_length(title) <= 120),
  opponent        text,
  competition     text,
  starts_at       timestamptz NOT NULL,
  city_code       char(3) NOT NULL REFERENCES cities(code) ON UPDATE CASCADE,
  venue_id        uuid REFERENCES venues(id) ON DELETE RESTRICT,
  venue_note      text,                             -- "[Local a definir]", "Phoenix Park"
  home_trip       text,                             -- "como voltar pra casa"
  description_md  text,
  cover_photo_id  uuid REFERENCES photos(id) ON DELETE SET NULL,
  status          text NOT NULL DEFAULT 'scheduled'
                  CHECK (status IN ('scheduled','voting','confirmed','postponed','cancelled')),
  is_published    boolean NOT NULL DEFAULT false,
  capacity        int CHECK (capacity > 0),
  spots_taken     int NOT NULL DEFAULT 0 CHECK (spots_taken >= 0),  -- manual na fase 0; COUNT(rsvps) na fase 1
  created_by, updated_by, created_at, updated_at,
  CONSTRAINT ck_events_match_opponent CHECK (kind <> 'match' OR opponent IS NOT NULL),
  CONSTRAINT ck_events_place CHECK (venue_id IS NOT NULL OR venue_note IS NOT NULL)
);
CREATE INDEX ix_events_board ON events (starts_at) WHERE is_published;

ALTER TABLE photos ADD CONSTRAINT fk_photos_event FOREIGN KEY (event_id) REFERENCES events(id) ON DELETE SET NULL;

-- ===== Posts =====
CREATE TABLE post_categories (
  id          smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  slug        text NOT NULL UNIQUE,
  name        text NOT NULL UNIQUE,
  sort_order  smallint NOT NULL DEFAULT 0
);

CREATE TABLE posts (
  id              uuid PRIMARY KEY,
  slug            text NOT NULL UNIQUE CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'),
  title           text NOT NULL CHECK (char_length(title) BETWEEN 1 AND 160),
  excerpt         text CHECK (char_length(excerpt) <= 300),
  body_md         text NOT NULL DEFAULT '',
  category_id     smallint REFERENCES post_categories(id) ON DELETE RESTRICT,
  cover_photo_id  uuid REFERENCES photos(id) ON DELETE SET NULL,
  status          text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','published','archived')),
  published_at    timestamptz,                      -- no futuro = agendado
  author_id       uuid REFERENCES users(id) ON DELETE SET NULL,
  created_by, updated_by, created_at, updated_at,
  CHECK (status <> 'published' OR published_at IS NOT NULL)
);
CREATE INDEX ix_posts_public ON posts (published_at DESC) WHERE status = 'published';

-- ===== História =====
CREATE TABLE milestones (
  id            uuid PRIMARY KEY,
  year          smallint NOT NULL CHECK (year BETWEEN 1900 AND 2100),
  text          text NOT NULL CHECK (char_length(text) <= 200),
  photo_id      uuid REFERENCES photos(id) ON DELETE SET NULL,
  sort_order    smallint NOT NULL DEFAULT 0,        -- desempate no mesmo ano
  is_published  boolean NOT NULL DEFAULT false,     -- só depois de checar a fonte
  created_by, updated_by, created_at, updated_at
);

-- ===== Configurações (linha única) =====
CREATE TABLE site_settings (
  id                    boolean PRIMARY KEY DEFAULT true CHECK (id),
  hero_photo_id         uuid REFERENCES photos(id) ON DELETE SET NULL,
  membership_fee_cents  int CHECK (membership_fee_cents >= 0),
  membership_currency   char(3) NOT NULL DEFAULT 'EUR',
  whatsapp_url          text CHECK (whatsapp_url ~ '^https://'),
  instagram_url         text CHECK (instagram_url ~ '^https://'),
  contact_email         citext,
  updated_by            uuid REFERENCES users(id) ON DELETE SET NULL,
  updated_at            timestamptz NOT NULL
);

-- ===== Sistema =====
CREATE TABLE audit_log (
  id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  occurred_at    timestamptz NOT NULL,
  actor_user_id  uuid REFERENCES users(id) ON DELETE SET NULL,
  action         text NOT NULL,                     -- create, update, delete, role_grant...
  entity_type    text NOT NULL,                     -- event, post, photo...
  entity_id      text NOT NULL,
  changes        jsonb                              -- {"title": ["antes", "depois"]}
);
CREATE INDEX ix_audit_entity ON audit_log (entity_type, entity_id, occurred_at DESC);

CREATE TABLE site_rebuild_requests (
  id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  requested_at  timestamptz NOT NULL,
  requested_by  uuid REFERENCES users(id) ON DELETE SET NULL,
  reason        text NOT NULL,
  triggered_at  timestamptz                         -- null = pendente
);
CREATE INDEX ix_site_rebuild_pending ON site_rebuild_requests (requested_at) WHERE triggered_at IS NULL;
```

`created_by, updated_by, created_at, updated_at` em todas as entidades editáveis são preenchidos pelo interceptor do EF. A versão para concorrência otimista é a coluna de sistema `xmin` (não aparece no DDL).

### Status do evento no painel da landing

Gravado: `status` (decisão humana) e `is_published`. O selo exibido é **derivado no front**, porque a landing é gerada no build e um selo calculado na API envelheceria no HTML:

1. `cancelled` / `postponed` → "Cancelado" / "Adiado"
2. `voting` → "Em votação"
3. `confirmed` com início nas próximas ~6 h → **"Embarcando"**
4. `capacity` preenchida → "`spots_taken` de `capacity` vagas"
5. `confirmed` → "Confirmado"; `scheduled` → "A confirmar"

## Fases 1–3 (esboço, não implementado)

```
-- Fase 1: sócios
members(user_id PK FK users, member_number int UNIQUE DEFAULT nextval('member_number_seq'),
        full_name, phone, city_code FK, joined_at, card_key_version smallint,
        photo_tag_opt_in boolean, created_at, updated_at)
event_rsvps(event_id FK, user_id FK, answer IN ('going','maybe','not_going'), updated_at,
            PK(event_id, user_id))
documents(id, title, r2_key UNIQUE, mime_type, visibility IN ('members','public'),
          sort_order, published_at, uploaded_by)

-- Fase 2: Stripe (fonte da verdade)
members.stripe_customer_id text UNIQUE
subscriptions(id, user_id FK, stripe_subscription_id UNIQUE, status (espelha o Stripe),
              current_period_end, cancel_at_period_end, last_event_created)
stripe_events(id text PK /* evt_... */, type, received_at, processed_at, error)

-- Fase 3
polls(id, title, description_md, is_secret, opens_at, closes_at, status, event_id NULL)
poll_options(id, poll_id FK, label, sort_order)
poll_participations(poll_id, user_id, voted_at, PK(poll_id, user_id))  -- quem votou
poll_ballots(id uuid v4, poll_id, option_id, user_id NULL)              -- o voto; sem user_id se secreta
benefits(id, venue_id FK NULL, title, description, discount_text, valid_until, sort_order, status)
photo_tags(photo_id, user_id, status IN ('approved','removed'), tagged_by, created_at,
           PK(photo_id, user_id))                                        -- marcação manual, opt-in
```

## Retenção (GDPR)

| Dado | Regra |
|---|---|
| `login_tokens` | apagados 24 h após expirar |
| `sessions` | 30 dias deslizantes, 90 absolutos; apagadas 7 dias após expirar ou serem revogadas |
| `audit_log` | 24 meses; na exclusão de conta, `changes = NULL` nos registros da própria pessoa |
| Conta excluída | anonimização imediata (e-mail `erased+<id>@invalid`, nome "Ex-sócio", papéis e sessões removidos) |
| Consentimento de foto retirado | `visibility = 'private'` na hora; purga no R2 em até 30 dias |
| `photos` `pending` | apagadas após 24 h |
| `stripe_events` | 90 dias |
| Backups (`pg_dump`) | 30 dias; dado apagado some dos backups nesse prazo |

## Seed (migration inicial)

- Cidades DUB, ORK, GWY com os textos atuais.
- Categorias: Encontros, Memória, Saudade.
- Linha única de `site_settings`.
- Marcos da história com `is_published = false` até confirmar as fontes.
- Primeiro admin via `ADMIN_BOOTSTRAP_EMAIL` no boot da API, nunca no repositório.
