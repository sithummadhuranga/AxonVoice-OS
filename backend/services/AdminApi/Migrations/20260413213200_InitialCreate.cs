using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AdminApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "axon");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "agent_purposes",
                schema: "axon",
                columns: table => new
                {
                    purpose_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_system_prompt = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_purposes", x => x.purpose_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "axon",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_knowledge_bases",
                schema: "axon",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_chunk = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(384)", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_knowledge_bases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_profiles",
                schema: "axon",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    purpose_id = table.Column<int>(type: "integer", nullable: false),
                    webhook_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    webhook_auth_token_encrypted = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "fk_agent_profiles_agent_purposes_purpose_id",
                        column: x => x.purpose_id,
                        principalSchema: "axon",
                        principalTable: "agent_purposes",
                        principalColumn: "purpose_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "axon",
                table: "agent_purposes",
                columns: new[] { "purpose_id", "base_system_prompt", "created_at_utc", "name" },
                values: new object[,]
                {
                    { 1, "You are an order processing agent for a retail store. Help customers check product availability and place orders. Always confirm order details before placing.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "E-Commerce Order Taking" },
                    { 2, "You are a receptionist scheduling medical appointments. Help patients find available slots and book appointments. Always confirm the patient's name, date, and service type.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Clinic Appointment Booking" },
                    { 3, "You are a helpful customer service agent. Answer questions based on the provided knowledge base. If you don't know the answer, say so honestly.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "General FAQ" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_profiles_purpose_id",
                schema: "axon",
                table: "agent_profiles",
                column: "purpose_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_profiles_tenant_id",
                schema: "axon",
                table: "agent_profiles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc",
                schema: "axon",
                table: "outbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_knowledge_bases_embedding",
                schema: "axon",
                table: "tenant_knowledge_bases",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_knowledge_bases_profile_id",
                schema: "axon",
                table: "tenant_knowledge_bases",
                column: "profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_profiles",
                schema: "axon");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "axon");

            migrationBuilder.DropTable(
                name: "tenant_knowledge_bases",
                schema: "axon");

            migrationBuilder.DropTable(
                name: "agent_purposes",
                schema: "axon");
        }
    }
}
