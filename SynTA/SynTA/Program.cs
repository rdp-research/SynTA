using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SynTA.Data;
using SynTA.Models.Domain;
using SynTA.Models.Testing;
using SynTA.Services.AI;
using SynTA.Services.AI.Prompts;
using SynTA.Services.Analytics;
using SynTA.Services.Database;
using SynTA.Services.Export;
using SynTA.Services.ImageProcessing;
using SynTA.Services.Testing;
using SynTA.Services.Utilities;
using SynTA.Services.Workflows;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SynTA application...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());

    builder.AddServiceDefaults();

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

    // Register database services
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IUserStoryService, UserStoryService>();
    builder.Services.AddScoped<IGherkinScenarioService, GherkinScenarioService>();
    builder.Services.AddScoped<ICypressScriptService, CypressScriptService>();
    builder.Services.AddScoped<ISettingsService, SettingsService>();

    // Register AI services - keyed services for different providers
    builder.Services.AddKeyedScoped<IAIGenerationService, OpenAIService>("OpenAI");
    builder.Services.AddKeyedScoped<IAIGenerationService, GeminiService>("Gemini");
    builder.Services.AddKeyedScoped<IAIGenerationService, OpenRouterService>("OpenRouter");

    // Register AI service factory
    builder.Services.AddScoped<IAIServiceFactory, AIServiceFactory>();

    // Register default AI service (based on configuration)
    builder.Services.AddScoped<IAIGenerationService>(sp =>
    {
        var factory = sp.GetRequiredService<IAIServiceFactory>();
        return factory.CreateService();
    });

    // Register prompt service for centralized prompt generation
    builder.Services.AddScoped<IPromptService, PromptService>();

    // Register image processing service for handling large screenshots
    builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

    // Register web scraping and HTML processing services
    builder.Services.AddSingleton<WebScraperService>(); // Singleton (reuses browser instance)
    builder.Services.AddSingleton<HtmlContentProcessor>(); // Singleton (stateless processor)

    // Register HTML context service as singleton (coordinates between scraper and processor)
    builder.Services.AddSingleton<IHtmlContextService, HtmlContextService>();

    // Register utility services (must be before FileExportService)
    builder.Services.AddScoped<IFileNameService, FileNameService>();

    // Register workflow services
    builder.Services.AddScoped<ITestGenerationWorkflowService, TestGenerationWorkflowService>();

    // Register analytics services
    builder.Services.AddScoped<IDashboardService, DashboardService>();

    // Register export services
    builder.Services.AddScoped<IFileExportService, FileExportService>();

    // Register Cypress test runner services
    builder.Services.Configure<CypressSettings>(
        builder.Configuration.GetSection(CypressSettings.SectionName));
    builder.Services.AddSingleton<ICypressLogParser, CypressLogParser>();
    builder.Services.AddSingleton<ICypressRunnerService, CypressRunnerService>();

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorComponents();

    var app = builder.Build();

    // Add Serilog request logging middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("UserId", httpContext.User?.Identity?.Name ?? "anonymous");
        };
    });

    app.MapDefaultEndpoints();

    // Seed roles
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            if (app.Environment.IsDevelopment())
            {
                Log.Information("Applying pending database migrations...");
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");
            }

            Log.Information("Seeding application roles...");
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await SeedRolesAsync(roleManager);
            Log.Information("Application roles seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding roles");
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        Log.Information("Running in Development environment");
        app.UseMigrationsEndPoint();
    }
    else
    {
        Log.Information("Running in {Environment} environment", app.Environment.EnvironmentName);
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();


    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    // Add area route
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages()
       .WithStaticAssets();

    Log.Information("SynTA application started successfully. Listening on configured endpoints...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("SynTA application shutting down...");
    Log.CloseAndFlush();
}

static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    // Create Admin role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Create User role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }
}
