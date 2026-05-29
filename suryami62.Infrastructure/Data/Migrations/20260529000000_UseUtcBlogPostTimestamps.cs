using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace suryami62.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseUtcBlogPostTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql(
                """
                ALTER TABLE "BlogPosts"
                ALTER COLUMN "Date" TYPE timestamp with time zone
                USING "Date" AT TIME ZONE 'UTC';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql(
                """
                ALTER TABLE "BlogPosts"
                ALTER COLUMN "Date" TYPE timestamp without time zone
                USING "Date" AT TIME ZONE 'UTC';
                """);
        }
    }
}
