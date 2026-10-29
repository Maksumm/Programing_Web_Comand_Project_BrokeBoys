using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Email_CategoryId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Subscriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            // Підписки, створені до появи власника, нікому не належать. Кожна отримує власний «legacy-<id>», щоб
            // не порушити новий унікальний індекс (одна категорія могла мати кілька різних пошт), і лишається
            // невидимою для всіх користувачів.
            migrationBuilder.Sql("UPDATE \"Subscriptions\" SET \"OwnerId\" = 'legacy-' || \"Id\"::text;");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OwnerId_CategoryId",
                table: "Subscriptions",
                columns: new[] { "OwnerId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OwnerId_CategoryId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Email_CategoryId",
                table: "Subscriptions",
                columns: new[] { "Email", "CategoryId" },
                unique: true);
        }
    }
}
