using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkItems.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_CreatedAt",
                table: "WorkItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Priority",
                table: "WorkItems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_UpdatedAt",
                table: "WorkItems",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_CreatedAt",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_Priority",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_UpdatedAt",
                table: "WorkItems");
        }
    }
}
