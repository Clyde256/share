using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using Vks.Server.Data;
using Vks.Server.Data.Seeds;
using Vks.Server.Hubs;
using Vks.Server.Models;
using Vks.Server.Objects;
using Vks.Server.Services;
using Serilog;
using Serilog.Events;



// App cesty
string workDir = Path.GetFullPath(".");
var appRootDir = AppDomain.CurrentDomain.BaseDirectory;

// Options pro spuštìní app jako služby
var opts = new WebApplicationOptions() { ContentRootPath = appRootDir, Args = args, ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName };
var builder = WebApplication.CreateBuilder(opts);

// Spuštìní jako služba
builder.Host.UseWindowsService();

// Konfigurace appsettings.json
var config = builder.Configuration;

// Parametry pro konfiguraci logu
var logFormat = "{Timestamp:yyyy/MM/dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

var loglevelDefault = config.GetValue<LogEventLevel>("Logging:LogLevel:Default", defaultValue: LogEventLevel.Information);
var loglevelSystem = config.GetValue<LogEventLevel>("Logging:LogLevel:System", defaultValue: LogEventLevel.Warning);
var loglevelMicrosoft = config.GetValue<LogEventLevel>("Logging:LogLevel:Microsoft", defaultValue: LogEventLevel.Warning);

var logFileName = config.GetValue<string>("Logging:LogFile:Name", defaultValue: "log_.txt");
var logMaxFileCount = config.GetValue<int>("Logging:LogFile:KeepCount", defaultValue: 3);
var logMaxByteFileSize = config.GetValue<int>("Logging:LogFile:MaxByteSize", defaultValue: 5242880);
var logRollingInterval = config.GetValue<RollingInterval>("Logging:LogFile:NewLogInterval", defaultValue: RollingInterval.Month);
var logLocationPath = config.GetValue<string>("Logging:LogFile:LocationPath", defaultValue: "./log");

// Úprava cesty k logu na absolutní cestu
if (!Path.IsPathRooted(logLocationPath)) 
{
    logLocationPath = Path.Combine(appRootDir, logLocationPath);
    logLocationPath = Path.GetFullPath(logLocationPath);
}

// Vytvoøení adresáøe pro log, pokud neexistuje
if (!Directory.Exists(logLocationPath)) 
{
    try
    {
        Directory.CreateDirectory(logLocationPath);
    }
    catch (Exception) 
    {
        System.Console.WriteLine($"Failed to create log folder {logLocationPath}.");
        return;
    }
}

// Konfigurace logu
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(new Serilog.Core.LoggingLevelSwitch(loglevelDefault))
    .MinimumLevel.Override("System", loglevelSystem)
    .MinimumLevel.Override("Microsoft", loglevelMicrosoft)
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: logFormat)
    .WriteTo.File(
        Path.Combine(logLocationPath, logFileName),
        rollingInterval: logRollingInterval,
        retainedFileCountLimit: logMaxFileCount,
        fileSizeLimitBytes: logMaxByteFileSize,
        rollOnFileSizeLimit: true,
        outputTemplate: logFormat
    )
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Host.UseSerilog();

// Zalogování spuštìní
Log.Information("\r\n __   ___  _____ \r\n \\ \\ / / |/ / __|\r\n  \\ V /| ' <\\__ \\\r\n   \\_/ |_|\\_\\___/\r\n");
Log.Information("Application was started");

// Pracovní adresáø
if (workDir != appRootDir)
{
    Log.Information($"Changing workdir from {workDir} to {appRootDir}");
    System.IO.Directory.SetCurrentDirectory(appRootDir);
    workDir = Path.GetFullPath(".");
}

Log.Information($"Work directory is {workDir}");
Log.Information($"Log directory is {logLocationPath}");

// Nastavení IP a portù
if (config.GetSection("Host").Exists())
{
    var hasAddress = config.GetSection("Host:PorHttp").Exists();
    var hasHttpPort = config.GetSection("Host:PorHttp").Exists();
    var hasHttpsPort = config.GetSection("Host:PortHttps").Exists();
    var hasCertPath = config.GetSection("Host:CertFilePath").Exists();
    var hasPwdPath = config.GetSection("Host:CertPwdFilePath").Exists();

    if (hasAddress && hasHttpPort) 
    {
        var address = config.GetValue<string>("Host:Address");
        var httpPort = config.GetValue<int>("Host:PorHttp");
        var httpsPort = config.GetValue<int>("Host:PortHttps");
        var httpsCertPath = config.GetValue<string>("Host:CertFilePath");
        var httpsPwdPath = config.GetValue<string>("Host:CertPwdFilePath");

        string httpsPwd = "";

        if (hasHttpsPort && hasCertPath && hasPwdPath) 
        {
            var cerExist = File.Exists(httpsCertPath);
            var pwdExist = File.Exists(httpsPwdPath);

            if (cerExist && pwdExist)
            {
                httpsPwd = File.ReadAllText(httpsPwdPath, Encoding.UTF8).TrimEnd();
            }
            else 
            {
                Log.Warning($"Not found certificate files: {httpsCertPath}, {httpsPwdPath}.");
                Log.Warning($"Certificate file full path: {Path.GetFullPath(httpsCertPath)}.");
                Log.Warning($"Password file full path: {Path.GetFullPath(httpsPwdPath)}.");
            }
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Parse(address), httpPort);

            if (httpsPwd.Length > 0)
            {
                options.Listen(IPAddress.Parse(address), httpsPort, listenOptions =>
                {
                    listenOptions.UseHttps(httpsCertPath, httpsPwd);
                });
            }
        });
    }
}

// App DB
var appDbConStr = builder.Configuration.GetConnectionString("AppDbConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(appDbConStr));

// AIS DB
var aisDbConStr = builder.Configuration.GetConnectionString("AisDbConnection");

builder.Services.AddDbContext<AisDbContext>(options =>
    options.UseSqlServer(aisDbConStr));

// Data Seeder
builder.Services.AddTransient<DataSeeder>();

// Exception filter
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Pravidla pro hesla
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout nastavení
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.AllowedForNewUsers = true;

    // Nastavení uživatele
    options.User.RequireUniqueEmail = false;
});

// Cookie nastavení
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// DI závislosti
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IRazorRendererHelper, RazorRendererHelper>();

builder.Services.AddScoped<Vks.Server.Services.SerialGenerator>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.Configure<SmtpParam>(builder.Configuration.GetSection("MailServer"));
builder.Services.AddScoped<MailNotifier>();

builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Konfigurace APP
var app = builder.Build();

// Komprese odpovìdí
app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Ostatní nastavení aplikace
if (config.GetSection("Host:RedirectHttps").Exists()) 
{
    var httpsRedirec = config.GetValue<bool>("Host:RedirectHttps");

    if (httpsRedirec) 
    {
        app.UseHttpsRedirection();
    }
}

// Další konfigurace
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapHub<NotifyHub>(NotifyHub.HubUrl);

app.MapFallbackToFile("index.html");

// Kontrola migrací
var appScope = app.Services.CreateScope();
using var dbContext = appScope.ServiceProvider.GetService<AppDbContext>();
var needMigration = dbContext.Database.GetPendingMigrations().Any();

if (needMigration)
{

    var applyMigration = args.Contains("-m");

    if (!applyMigration) 
    {
        Log.Error("Database requires migration. Run the application with the -m parameter of its execution.");
        return;
    }

    Log.Information("Starting database migration.");
    dbContext.Database.Migrate();
    Log.Information("Database migration was completed.");
}

// Data seeder - generovani deefaultnich dat
try
{
    Log.Information("Starting seed creation.");
    var scope = app.Services.CreateScope();
    var dataSeeder = scope.ServiceProvider.GetService<DataSeeder>();
    await dataSeeder.SeedAsync();
    Log.Information("Seed creation was completed.");
}
catch (Exception ex)
{
    Log.Error(ex.Message);
    return;
}

// Inicializace SN generatoru
Log.Information("Initializing SN generator.");
var snGen = appScope.ServiceProvider.GetService<Vks.Server.Services.SerialGenerator>();
snGen.Init();
Log.Information("SN generator was successfully initialized.");

// Nastartování web app
Log.Information("Starting web application.");

try
{
    app.Run();
}
catch (Exception ex) 
{
    Log.Error(ex.Message);
}