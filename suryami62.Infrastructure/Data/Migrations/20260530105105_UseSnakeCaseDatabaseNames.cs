using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace suryami62.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCaseDatabaseNames : Migration
    {
        private static readonly string[] AspNetUserTokenKeyColumns = ["user_id", "login_provider", "name"];

        private static readonly string[] AspNetUserRoleKeyColumns = ["user_id", "role_id"];

        private static readonly string[] AspNetUserLoginKeyColumns = ["login_provider", "provider_key"];

        private static readonly string[] LegacyAspNetUserTokenKeyColumns = ["UserId", "LoginProvider", "Name"];

        private static readonly string[] LegacyAspNetUserRoleKeyColumns = ["UserId", "RoleId"];

        private static readonly string[] LegacyAspNetUserLoginKeyColumns = ["LoginProvider", "ProviderKey"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Settings",
                table: "Settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JourneyHistories",
                table: "JourneyHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BlogPosts",
                table: "BlogPosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Settings",
                newName: "settings");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "projects");

            migrationBuilder.RenameTable(
                name: "JourneyHistories",
                newName: "journey_histories");

            migrationBuilder.RenameTable(
                name: "BlogPosts",
                newName: "blog_posts");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "asp_net_user_tokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "asp_net_users");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "asp_net_user_roles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "asp_net_user_logins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "asp_net_user_claims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "asp_net_roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "asp_net_role_claims");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "settings",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "settings",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "settings",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Settings_Key",
                table: "settings",
                newName: "ix_settings_key");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "projects",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "projects",
                newName: "tags");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "projects",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "projects",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RepoUrl",
                table: "projects",
                newName: "repo_url");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "projects",
                newName: "image_url");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "projects",
                newName: "display_order");

            migrationBuilder.RenameColumn(
                name: "DemoUrl",
                table: "projects",
                newName: "demo_url");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_DisplayOrder",
                table: "projects",
                newName: "ix_projects_display_order");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "journey_histories",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "journey_histories",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "journey_histories",
                newName: "section");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "journey_histories",
                newName: "period");

            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "journey_histories",
                newName: "organization");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "journey_histories",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "journey_histories",
                newName: "display_order");

            migrationBuilder.RenameIndex(
                name: "IX_JourneyHistories_Section_DisplayOrder",
                table: "journey_histories",
                newName: "ix_journey_histories_section_display_order");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "blog_posts",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "blog_posts",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "blog_posts",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "blog_posts",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "blog_posts",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "blog_posts",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "blog_posts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsPublished",
                table: "blog_posts",
                newName: "is_published");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "blog_posts",
                newName: "image_url");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_Title",
                table: "blog_posts",
                newName: "ix_blog_posts_title");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_Summary",
                table: "blog_posts",
                newName: "ix_blog_posts_summary");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_Slug",
                table: "blog_posts",
                newName: "ix_blog_posts_slug");

            migrationBuilder.RenameIndex(
                name: "IX_BlogPosts_IsPublished_Date",
                table: "blog_posts",
                newName: "ix_blog_posts_is_published_date");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "asp_net_user_tokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "asp_net_user_tokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "asp_net_user_tokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "asp_net_users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "asp_net_users",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "asp_net_users",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "asp_net_users",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "asp_net_users",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "asp_net_users",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "asp_net_users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "asp_net_users",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "asp_net_users",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "asp_net_users",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "asp_net_users",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "asp_net_users",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "asp_net_users",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "asp_net_users",
                newName: "access_failed_count");

            migrationBuilder.RenameIndex(
                name: "UserNameIndex",
                table: "asp_net_users",
                newName: "ux_asp_net_users_normalized_user_name");

            migrationBuilder.RenameIndex(
                name: "EmailIndex",
                table: "asp_net_users",
                newName: "ix_asp_net_users_normalized_email");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "asp_net_user_roles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_roles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "asp_net_user_roles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_logins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "asp_net_user_logins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "asp_net_user_logins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "asp_net_user_logins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "asp_net_user_logins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_user_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "asp_net_user_claims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "asp_net_user_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "asp_net_user_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "asp_net_user_claims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "asp_net_roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "asp_net_roles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "asp_net_roles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameIndex(
                name: "RoleNameIndex",
                table: "asp_net_roles",
                newName: "ux_asp_net_roles_normalized_name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "asp_net_role_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "asp_net_role_claims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "asp_net_role_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "asp_net_role_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "asp_net_role_claims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_settings",
                table: "settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_projects",
                table: "projects",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_journey_histories",
                table: "journey_histories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_blog_posts",
                table: "blog_posts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "asp_net_user_tokens",
                columns: AspNetUserTokenKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "asp_net_users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "asp_net_user_roles",
                columns: AspNetUserRoleKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "asp_net_user_logins",
                columns: AspNetUserLoginKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "asp_net_user_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "asp_net_roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "asp_net_role_claims",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "asp_net_role_claims",
                column: "role_id",
                principalTable: "asp_net_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "asp_net_user_claims",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "asp_net_user_logins",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id",
                principalTable: "asp_net_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "asp_net_user_roles",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "asp_net_user_tokens",
                column: "user_id",
                principalTable: "asp_net_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "asp_net_role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "asp_net_user_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "asp_net_user_logins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "asp_net_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "asp_net_user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "asp_net_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_settings",
                table: "settings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_projects",
                table: "projects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_journey_histories",
                table: "journey_histories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_blog_posts",
                table: "blog_posts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "asp_net_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "asp_net_user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "asp_net_user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "asp_net_user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "asp_net_user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "asp_net_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "asp_net_role_claims");

            migrationBuilder.RenameTable(
                name: "settings",
                newName: "Settings");

            migrationBuilder.RenameTable(
                name: "projects",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "journey_histories",
                newName: "JourneyHistories");

            migrationBuilder.RenameTable(
                name: "blog_posts",
                newName: "BlogPosts");

            migrationBuilder.RenameTable(
                name: "asp_net_users",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "asp_net_user_tokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "asp_net_user_roles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_user_logins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "asp_net_user_claims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "asp_net_roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "asp_net_role_claims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "Settings",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "key",
                table: "Settings",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Settings",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "ix_settings_key",
                table: "Settings",
                newName: "IX_Settings_Key");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Projects",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "tags",
                table: "Projects",
                newName: "Tags");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Projects",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Projects",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "repo_url",
                table: "Projects",
                newName: "RepoUrl");

            migrationBuilder.RenameColumn(
                name: "image_url",
                table: "Projects",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "display_order",
                table: "Projects",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "demo_url",
                table: "Projects",
                newName: "DemoUrl");

            migrationBuilder.RenameIndex(
                name: "ix_projects_display_order",
                table: "Projects",
                newName: "IX_Projects_DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "JourneyHistories",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "JourneyHistories",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "section",
                table: "JourneyHistories",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "period",
                table: "JourneyHistories",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "organization",
                table: "JourneyHistories",
                newName: "Organization");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "JourneyHistories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "display_order",
                table: "JourneyHistories",
                newName: "DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "ix_journey_histories_section_display_order",
                table: "JourneyHistories",
                newName: "IX_JourneyHistories_Section_DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "BlogPosts",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "BlogPosts",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "slug",
                table: "BlogPosts",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "BlogPosts",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "BlogPosts",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "BlogPosts",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BlogPosts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_published",
                table: "BlogPosts",
                newName: "IsPublished");

            migrationBuilder.RenameColumn(
                name: "image_url",
                table: "BlogPosts",
                newName: "ImageUrl");

            migrationBuilder.RenameIndex(
                name: "ix_blog_posts_title",
                table: "BlogPosts",
                newName: "IX_BlogPosts_Title");

            migrationBuilder.RenameIndex(
                name: "ix_blog_posts_summary",
                table: "BlogPosts",
                newName: "IX_BlogPosts_Summary");

            migrationBuilder.RenameIndex(
                name: "ix_blog_posts_slug",
                table: "BlogPosts",
                newName: "IX_BlogPosts_Slug");

            migrationBuilder.RenameIndex(
                name: "ix_blog_posts_is_published_date",
                table: "BlogPosts",
                newName: "IX_BlogPosts_IsPublished_Date");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameIndex(
                name: "ux_asp_net_users_normalized_user_name",
                table: "AspNetUsers",
                newName: "UserNameIndex");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_users_normalized_email",
                table: "AspNetUsers",
                newName: "EmailIndex");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameIndex(
                name: "ux_asp_net_roles_normalized_name",
                table: "AspNetRoles",
                newName: "RoleNameIndex");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settings",
                table: "Settings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JourneyHistories",
                table: "JourneyHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BlogPosts",
                table: "BlogPosts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: LegacyAspNetUserTokenKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: LegacyAspNetUserRoleKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: LegacyAspNetUserLoginKeyColumns);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
