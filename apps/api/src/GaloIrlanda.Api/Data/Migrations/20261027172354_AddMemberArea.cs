using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GaloIrlanda.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "member_number_seq");

            migrationBuilder.CreateTable(
                name: "benefits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    headline = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    highlight = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_benefits", x => x.id);
                    table.ForeignKey(
                        name: "fk_benefits_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_benefits_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_benefits_venues_venue_id",
                        column: x => x.venue_id,
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_rsvps",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_rsvps", x => new { x.event_id, x.user_id });
                    table.CheckConstraint("ck_event_rsvps_answer", "answer IN ('going', 'maybe', 'not_going')");
                    table.ForeignKey(
                        name: "fk_event_rsvps_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_rsvps_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_number = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('member_number_seq')"),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    city_code = table.Column<string>(type: "char(3)", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    referral_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    referred_by = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_tag_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    card_key_version = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_members_cities_city_code",
                        column: x => x.city_code,
                        principalTable: "cities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_members_users_referred_by",
                        column: x => x.referred_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "photo_tags",
                columns: table => new
                {
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tagged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photo_tags", x => new { x.photo_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_photo_tags_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_photo_tags_users_tagged_by",
                        column: x => x.tagged_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_photo_tags_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_secret = table.Column<bool>(type: "boolean", nullable: false),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    hand_note = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_polls", x => x.id);
                    table.CheckConstraint("ck_polls_period", "closes_at > opens_at");
                    table.ForeignKey(
                        name: "fk_polls_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_polls_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stripe_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stripe_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "text", nullable: false),
                    stripe_subscription_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    current_period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    last_event_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_poll_options_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_participations",
                columns: table => new
                {
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_participations", x => new { x.poll_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_poll_participations_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_poll_participations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_ballots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_ballots", x => x.id);
                    table.ForeignKey(
                        name: "fk_poll_ballots_poll_options_option_id",
                        column: x => x.option_id,
                        principalTable: "poll_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_poll_ballots_polls_poll_id",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_poll_ballots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_benefits_created_by",
                table: "benefits",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_benefits_updated_by",
                table: "benefits",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_benefits_venue_id",
                table: "benefits",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_created_by",
                table: "documents",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_documents_storage_key",
                table: "documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_updated_by",
                table: "documents",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_event_rsvps_user_id",
                table: "event_rsvps",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_members_city_code",
                table: "members",
                column: "city_code");

            migrationBuilder.CreateIndex(
                name: "ix_members_member_number",
                table: "members",
                column: "member_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_referral_code",
                table: "members",
                column: "referral_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_referred_by",
                table: "members",
                column: "referred_by");

            migrationBuilder.CreateIndex(
                name: "ix_photo_tags_tagged_by",
                table: "photo_tags",
                column: "tagged_by");

            migrationBuilder.CreateIndex(
                name: "ix_photo_tags_user_id",
                table: "photo_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_ballots_option_id",
                table: "poll_ballots",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_ballots_poll_id",
                table: "poll_ballots",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_ballots_user_id",
                table: "poll_ballots",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_options_poll_id",
                table: "poll_options",
                column: "poll_id");

            migrationBuilder.CreateIndex(
                name: "ix_poll_participations_user_id",
                table: "poll_participations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_polls_created_by",
                table: "polls",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_polls_updated_by",
                table: "polls",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_stripe_customer_id",
                table: "subscriptions",
                column: "stripe_customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_stripe_subscription_id",
                table: "subscriptions",
                column: "stripe_subscription_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_user_id",
                table: "subscriptions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benefits");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "event_rsvps");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "photo_tags");

            migrationBuilder.DropTable(
                name: "poll_ballots");

            migrationBuilder.DropTable(
                name: "poll_participations");

            migrationBuilder.DropTable(
                name: "stripe_events");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "polls");

            migrationBuilder.DropSequence(
                name: "member_number_seq");
        }
    }
}
