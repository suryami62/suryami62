using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace suryami62.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnBlogPostSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Create unique index on Slug column to prevent duplicate slugs
            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Remove the unique index
            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts");
        }
    }
}
