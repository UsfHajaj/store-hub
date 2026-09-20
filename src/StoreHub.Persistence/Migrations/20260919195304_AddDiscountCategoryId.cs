using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ProductDiscounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscounts_CategoryId",
                table: "ProductDiscounts",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDiscounts_ProductCategories_CategoryId",
                table: "ProductDiscounts",
                column: "CategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductDiscounts_ProductCategories_CategoryId",
                table: "ProductDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_ProductDiscounts_CategoryId",
                table: "ProductDiscounts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProductDiscounts");
        }
    }
}
