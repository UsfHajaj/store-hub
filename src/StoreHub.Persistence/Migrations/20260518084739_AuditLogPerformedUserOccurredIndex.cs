using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogPerformedUserOccurredIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedByUserId_OccurredOnUtc",
                table: "AuditLogs",
                columns: new[] { "PerformedByUserId", "OccurredOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedByUserId_OccurredOnUtc",
                table: "AuditLogs");
        }
    }
}
