using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GaloIrlanda.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "post_categories",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    erased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_erased", "(status = 'erased') = (erased_at IS NOT NULL)");
                    table.CheckConstraint("ck_users_status", "status IN ('active', 'disabled', 'erased')");
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "login_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_login_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    ticket = table.Column<byte[]>(type: "bytea", nullable: false),
                    user_agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_rebuild_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_rebuild_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_rebuild_requests_users_requested_by",
                        column: x => x.requested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role });
                    table.CheckConstraint("ck_user_roles_role", "role IN ('admin', 'editor', 'socio')");
                    table.ForeignKey(
                        name: "fk_user_roles_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    code = table.Column<string>(type: "char(3)", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    blurb = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.code);
                    table.CheckConstraint("ck_cities_code", "code ~ '^[A-Z]{3}$'");
                    table.ForeignKey(
                        name: "fk_cities_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cities_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_code = table.Column<string>(type: "char(3)", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    neighborhood = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    maps_url = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    is_partner = table.Column<bool>(type: "boolean", nullable: false),
                    shows_with_sound = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                    table.CheckConstraint("ck_venues_maps_url", "maps_url ~ '^https://'");
                    table.CheckConstraint("ck_venues_website_url", "website_url ~ '^https://'");
                    table.ForeignKey(
                        name: "fk_venues_cities_city_code",
                        column: x => x.city_code,
                        principalTable: "cities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_venues_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_venues_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    opponent = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    competition = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    city_code = table.Column<string>(type: "char(3)", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    venue_note = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    home_trip = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description_md = table.Column<string>(type: "text", nullable: true),
                    cover_photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    spots_taken = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.CheckConstraint("ck_events_capacity", "capacity IS NULL OR capacity > 0");
                    table.CheckConstraint("ck_events_kind", "kind IN ('match', 'meetup')");
                    table.CheckConstraint("ck_events_match_opponent", "kind <> 'match' OR opponent IS NOT NULL");
                    table.CheckConstraint("ck_events_place", "venue_id IS NOT NULL OR venue_note IS NOT NULL");
                    table.CheckConstraint("ck_events_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_events_spots_taken", "spots_taken >= 0");
                    table.CheckConstraint("ck_events_status", "status IN ('scheduled', 'voting', 'confirmed', 'postponed', 'cancelled')");
                    table.ForeignKey(
                        name: "fk_events_cities_city_code",
                        column: x => x.city_code,
                        principalTable: "cities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_events_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_events_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_status = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    alt_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    taken_on = table.Column<DateOnly>(type: "date", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    consent_basis = table.Column<string>(type: "text", nullable: false),
                    consent_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    consent_confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    wall_featured = table.Column<bool>(type: "boolean", nullable: false),
                    wall_sort_order = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photos", x => x.id);
                    table.CheckConstraint("ck_photos_consent", "consent_basis NOT IN ('consent_recorded', 'no_identifiable_people') OR consent_confirmed_at IS NOT NULL");
                    table.CheckConstraint("ck_photos_consent_basis", "consent_basis IN ('pending', 'no_identifiable_people', 'consent_recorded', 'withdrawn')");
                    table.CheckConstraint("ck_photos_dimensions", "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
                    table.CheckConstraint("ck_photos_public_ok", "visibility <> 'public' OR (upload_status = 'ready' AND alt_text IS NOT NULL AND consent_basis IN ('no_identifiable_people', 'consent_recorded'))");
                    table.CheckConstraint("ck_photos_upload_status", "upload_status IN ('pending', 'ready')");
                    table.CheckConstraint("ck_photos_visibility", "visibility IN ('public', 'members', 'private')");
                    table.CheckConstraint("ck_photos_wall", "NOT wall_featured OR visibility = 'public'");
                    table.ForeignKey(
                        name: "fk_photos_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_photos_users_consent_confirmed_by",
                        column: x => x.consent_confirmed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_photos_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_photos_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_milestones", x => x.id);
                    table.CheckConstraint("ck_milestones_year", "year BETWEEN 1900 AND 2100");
                    table.ForeignKey(
                        name: "fk_milestones_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_milestones_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_milestones_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    excerpt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body_md = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<short>(type: "smallint", nullable: true),
                    cover_photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                    table.CheckConstraint("ck_posts_published_at", "status <> 'published' OR published_at IS NOT NULL");
                    table.CheckConstraint("ck_posts_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_posts_status", "status IN ('draft', 'published', 'archived')");
                    table.ForeignKey(
                        name: "fk_posts_photos_cover_photo_id",
                        column: x => x.cover_photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_posts_post_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_posts_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_posts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_posts_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    id = table.Column<bool>(type: "boolean", nullable: false),
                    hero_photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    membership_fee_cents = table.Column<int>(type: "integer", nullable: true),
                    membership_currency = table.Column<string>(type: "char(3)", nullable: false),
                    whatsapp_url = table.Column<string>(type: "text", nullable: true),
                    instagram_url = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "citext", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_settings", x => x.id);
                    table.CheckConstraint("ck_site_settings_fee", "membership_fee_cents IS NULL OR membership_fee_cents >= 0");
                    table.CheckConstraint("ck_site_settings_instagram", "instagram_url ~ '^https://'");
                    table.CheckConstraint("ck_site_settings_single_row", "id");
                    table.CheckConstraint("ck_site_settings_whatsapp", "whatsapp_url ~ '^https://'");
                    table.ForeignKey(
                        name: "fk_site_settings_photos_hero_photo_id",
                        column: x => x.hero_photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_site_settings_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_actor_user_id",
                table: "audit_log",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_type_entity_id_occurred_at",
                table: "audit_log",
                columns: new[] { "entity_type", "entity_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_cities_created_by",
                table: "cities",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_cities_photo_id",
                table: "cities",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_updated_by",
                table: "cities",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_events_city_code",
                table: "events",
                column: "city_code");

            migrationBuilder.CreateIndex(
                name: "ix_events_cover_photo_id",
                table: "events",
                column: "cover_photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by",
                table: "events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_events_slug",
                table: "events",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_starts_at",
                table: "events",
                column: "starts_at",
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_events_updated_by",
                table: "events",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_events_venue_id",
                table: "events",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_login_tokens_token_hash",
                table: "login_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_login_tokens_user_id_created_at",
                table: "login_tokens",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_milestones_created_by",
                table: "milestones",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_photo_id",
                table: "milestones",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_updated_by",
                table: "milestones",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_milestones_year_sort_order",
                table: "milestones",
                columns: new[] { "year", "sort_order" },
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_photos_consent_confirmed_by",
                table: "photos",
                column: "consent_confirmed_by");

            migrationBuilder.CreateIndex(
                name: "ix_photos_created_by",
                table: "photos",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_photos_event_id",
                table: "photos",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_photos_updated_by",
                table: "photos",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_photos_wall_sort_order",
                table: "photos",
                column: "wall_sort_order",
                filter: "wall_featured AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_name",
                table: "post_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_slug",
                table: "post_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_author_id",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_category_id",
                table: "posts",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_cover_photo_id",
                table: "posts",
                column: "cover_photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_created_by",
                table: "posts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_posts_published_at",
                table: "posts",
                column: "published_at",
                descending: new bool[0],
                filter: "status = 'published'");

            migrationBuilder.CreateIndex(
                name: "ix_posts_slug",
                table: "posts",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_updated_by",
                table: "posts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_token_hash",
                table: "sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id",
                table: "sessions",
                column: "user_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_site_rebuild_requests_requested_at",
                table: "site_rebuild_requests",
                column: "requested_at",
                filter: "triggered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_site_rebuild_requests_requested_by",
                table: "site_rebuild_requests",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_site_settings_hero_photo_id",
                table: "site_settings",
                column: "hero_photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_settings_updated_by",
                table: "site_settings",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_granted_by",
                table: "user_roles",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venues_city_code_name",
                table: "venues",
                columns: new[] { "city_code", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venues_created_by",
                table: "venues",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_venues_updated_by",
                table: "venues",
                column: "updated_by");

            migrationBuilder.AddForeignKey(
                name: "fk_cities_photos_photo_id",
                table: "cities",
                column: "photo_id",
                principalTable: "photos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_events_photos_cover_photo_id",
                table: "events",
                column: "cover_photo_id",
                principalTable: "photos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Seed: dados iniciais que voluntários editam depois (ADR 0015: SQL idempotente, nunca HasData).
            migrationBuilder.Sql("""
                INSERT INTO cities (code, name, blurb, sort_order, is_active, created_at, updated_at) VALUES
                  ('DUB', 'Dublin', 'Sede da bagunça. Todo jogo no [Pub parceiro], pelada no Phoenix Park.', 1, true, now(), now()),
                  ('ORK', 'Cork', 'Turma pequena e barulhenta. Jogos grandes no [Pub parceiro].', 2, true, now(), now()),
                  ('GWY', 'Galway', 'Ainda é um grupinho. Bora crescer? Chama no grupo.', 3, true, now(), now())
                ON CONFLICT DO NOTHING;

                INSERT INTO post_categories (slug, name, sort_order) VALUES
                  ('encontros', 'Encontros', 1),
                  ('memoria', 'Memória', 2),
                  ('saudade', 'Saudade', 3)
                ON CONFLICT DO NOTHING;

                INSERT INTO site_settings (id, membership_currency, created_at, updated_at)
                VALUES (true, 'EUR', now(), now())
                ON CONFLICT DO NOTHING;

                -- Marcos entram despublicados até alguém conferir a fonte.
                INSERT INTO milestones (id, year, text, sort_order, is_published, created_at, updated_at) VALUES
                  (gen_random_uuid(), 1908, 'Nasce o Clube Atlético Mineiro, em BH.', 0, false, now(), now()),
                  (gen_random_uuid(), 1950, 'Campeões do Gelo na Europa. [confirmar detalhes]', 0, false, now(), now()),
                  (gen_random_uuid(), 1971, 'Primeiro campeão brasileiro.', 0, false, now(), now()),
                  (gen_random_uuid(), 2013, 'Libertadores. Eu acredito.', 0, false, now(), now()),
                  (gen_random_uuid(), 2021, 'Brasileiro e Copa do Brasil. A gente viu daqui.', 0, false, now(), now());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cities_users_created_by",
                table: "cities");

            migrationBuilder.DropForeignKey(
                name: "fk_cities_users_updated_by",
                table: "cities");

            migrationBuilder.DropForeignKey(
                name: "fk_events_users_created_by",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_events_users_updated_by",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "fk_photos_users_consent_confirmed_by",
                table: "photos");

            migrationBuilder.DropForeignKey(
                name: "fk_photos_users_created_by",
                table: "photos");

            migrationBuilder.DropForeignKey(
                name: "fk_photos_users_updated_by",
                table: "photos");

            migrationBuilder.DropForeignKey(
                name: "fk_venues_users_created_by",
                table: "venues");

            migrationBuilder.DropForeignKey(
                name: "fk_venues_users_updated_by",
                table: "venues");

            migrationBuilder.DropForeignKey(
                name: "fk_cities_photos_photo_id",
                table: "cities");

            migrationBuilder.DropForeignKey(
                name: "fk_events_photos_cover_photo_id",
                table: "events");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "login_tokens");

            migrationBuilder.DropTable(
                name: "milestones");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "site_rebuild_requests");

            migrationBuilder.DropTable(
                name: "site_settings");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "post_categories");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "venues");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
