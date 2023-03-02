using EFIdentityServer;
using EFIdentityServer.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("DefaultConnection");

var migrationAssembly = typeof(Config).Assembly.GetName().Name;

builder.Services.AddRazorPages();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, opt => opt.MigrationsAssembly(migrationAssembly));
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    options.EmitStaticAudienceClaim = true;
}).AddConfigurationStore(options => options.ConfigureDbContext = b => b.UseSqlServer(connectionString, opt => opt.MigrationsAssembly(migrationAssembly)))
  .AddOperationalStore(options => options.ConfigureDbContext = b => b.UseSqlServer(connectionString, opt => opt.MigrationsAssembly(migrationAssembly)))
  .AddAspNetIdentity<IdentityUser>();

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.MinimumLevel.Debug()
      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
      .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
      .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
      .MinimumLevel.Override("System", LogEventLevel.Warning)
      .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
            theme: AnsiConsoleTheme.Code
       ).Enrich.FromLogContext();
});


var app = builder.Build();

app.UseIdentityServer();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();


// this seeding is only for the template to bootstrap the DB and users.
// in production you will likely want a different approach.
if (args.Contains("/seed"))
{
    Log.Information("Seeding database...");
    SeedData.EnsureSeedData(app);
    Log.Information("Done seeding database. Exiting.");
    return;
}

app.Run();
