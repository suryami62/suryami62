// ============================================================================
// PROGRAM.CS - Application Entry Point
// ============================================================================
// This is the starting point of the web application.
// When you run "dotnet run", this file executes first.
//
// WHAT IT DOES:
// 1. Sets up PostgreSQL compatibility mode
// 2. Creates a "builder" to configure the application
// 3. Registers all services (database, authentication, caching, etc.)
// 4. Builds the application
// 5. Configures the middleware pipeline (how requests are handled)
// 6. Starts the web server
// ============================================================================

#region

using suryami62.Startup; // Extension methods for configuration

#endregion


// Enable legacy timestamp behavior for PostgreSQL (Npgsql).
// This ensures DateTime values work correctly with the database.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Step 1: Create a builder - this is the configuration phase
// WebApplication.CreateBuilder sets up:
// - Configuration (appsettings.json, environment variables)
// - Logging
// - Web server (Kestrel)
var builder = WebApplication.CreateBuilder(args);

// Step 2: Register all application services
// This extension method (defined in Startup folder) adds:
// - Razor Components (Blazor)
// - Authentication and Identity
// - Database context
// - Redis caching
// - Rate limiting
// - Response compression
builder.Services.AddWebApplicationServices(builder.Configuration);

// Step 3: Build the application
// This creates the actual web application instance with all services configured
var app = builder.Build();

// Step 4: Configure the middleware pipeline
// Middleware processes HTTP requests in order:
// 1. Exception handling
// 2. HTTPS redirection
// 3. Security headers
// 4. Static files
// 5. Authentication
// 6. Razor Components
app.UseWebStartupPipeline();

// Step 5: Map endpoints (routes)
// This sets up:
// - Static assets (CSS, JS, images)
// - Razor Components pages
// - API endpoints
// - SEO endpoints (sitemap, robots.txt)
app.MapWebEndpoints();

// Step 6: Apply database migrations
// This ensures the database schema is up to date
app.ApplyDatabaseMigrations();

// Step 7: Start the web server and listen for requests
// This blocks (waits) until the application is shut down
app.Run();