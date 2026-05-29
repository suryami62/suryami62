using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace suryami62.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        private static readonly string[] BlogPostPublishedDateIndexColumns = ["IsPublished", "Date"];

        private static readonly bool[] BlogPostPublishedDateIndexDescending = [false, true];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DisplayOrder",
                table: "Projects",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsPublished_Date",
                table: "BlogPosts",
                columns: BlogPostPublishedDateIndexColumns,
                descending: BlogPostPublishedDateIndexDescending);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_Projects_DisplayOrder",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_IsPublished_Date",
                table: "BlogPosts");
        }
    }
}
