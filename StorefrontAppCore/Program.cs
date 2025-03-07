using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StorefrontAppCore.Data;
using StorefrontAppCore.Models;
using StorefrontAppCore.Utilities;
using NReco.Logging.File;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var supabaseConnectionString = builder.Configuration.GetConnectionString("SupabaseConnection") ?? throw new InvalidOperationException("Connection string 'SupabaseConnection' not found.");
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? throw new InvalidOperationException("Database provider not configured.");

// Dynamically choose the provider for multiple providers and their migrations for EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (provider)
    {
        case "SqlServer":
            options.UseSqlServer(connectionString);
            break;
        case "Postgresql":
            options.UseNpgsql(supabaseConnectionString);
            break;
        default:
            throw new InvalidOperationException($"Unsupported provider: {provider}");
    }
});
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Adding and configuring the Identity functionality
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // Confirmation settings
    options.SignIn.RequireConfirmedAccount = false;

    // Two-Factor Authentication settings
    options.Tokens.AuthenticatorIssuer = "Slush Storefront";
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Registering the EmailService class as the implementation of IEmailSender
builder.Services.AddTransient<IEmailSender, EmailService>();

// Adding the configuration from appsettings.json and secrets.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

// Binding the appsettings.json section to a POCO class
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Registering the Cryptography class
builder.Services.AddScoped<Cryptography>();

// Adding a custom logger, the NReco.Logging.File Nuget package 
builder.Services.AddLogging(loggingBuilder => {
    var loggingSection = builder.Configuration.GetSection("Logging");
    loggingBuilder.AddFile(loggingSection);
});

// Adding the services for MVC and Razor syntax
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configuring the application cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/LogOff";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configuring the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
