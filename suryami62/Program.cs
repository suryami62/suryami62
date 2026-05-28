#region

using suryami62.Startup;

#endregion

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseWebStartupPipeline();

app.MapWebEndpoints();

await app.ApplyDatabaseMigrationsAsync().ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);