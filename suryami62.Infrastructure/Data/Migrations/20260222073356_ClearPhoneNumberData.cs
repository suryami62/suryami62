using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace suryami62.Data.Migrations
{
    /// <inheritdoc />
    public sealed partial class ClearPhoneNumberData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            System.ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql("""
                UPDATE \"AspNetUsers\"
                SET \"PhoneNumber\" = NULL,
                    \"PhoneNumberConfirmed\" = FALSE
                WHERE \"PhoneNumber\" IS NOT NULL
                   OR \"PhoneNumberConfirmed\" = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            System.ArgumentNullException.ThrowIfNull(migrationBuilder);

            // Intentionally empty: clearing phone numbers is not reversible.
        }
    }
}
