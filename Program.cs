#nullable disable

using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ================================
// 🔧 ZENTRALES DEBUG-SYSTEM
// ================================
// Hier einmal auf true setzen = Debug überall aktiv
const bool DEBUG_MODE = false;

// Debug-Konfiguration für Visual Studio Debug Console
if (DEBUG_MODE)
{
    builder.Services.Configure<DebugOptions>(options =>
    {
        options.EnableDebug = true;
        options.EnableFormDebug = true;
        options.EnableControllerDebug = true;
        options.EnableJavaScriptDebug = true;
    });

    // Custom Logger nur für unsere Debug-Nachrichten
    builder.Logging.ClearProviders();
    builder.Logging.AddDebug(); // Geht direkt zur VS Debug Console
    builder.Logging.AddConsole();

    // Debug-Level setzen
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Services.Configure<DebugOptions>(options =>
    {
        options.EnableDebug = false;
        options.EnableFormDebug = false;
        options.EnableControllerDebug = false;
        options.EnableJavaScriptDebug = false;
    });
}

// Services hinzufügen
builder.Services.AddControllersWithViews();

// API Controller Support
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Session für Zeiträume-Management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// CORS für lokale Entwicklung
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// HttpClient für Feiertag-API
builder.Services.AddHttpClient<FeiertagService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "WochennachweisGenerator/1.0");
});

// Services registrieren
builder.Services.AddSingleton<FeiertagService>();
builder.Services.AddScoped<WochennachweisGenerator>();
builder.Services.AddScoped<DebugService>();

var app = builder.Build();

// Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseCors("AllowLocalhost");
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// API Routes zuerst
app.MapControllers();

// Standard MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Debug-Startup-Informationen - NUR in VS Debug Console
if (DEBUG_MODE)
{
    // WICHTIG: Nur System.Diagnostics.Debug.WriteLine geht zur VS Debug Console!
    // Console.WriteLine und ILogger gehen ins Output-Fenster
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine("🚀 WOCHENNACHWEIS-GENERATOR GESTARTET");
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine($"🔧 Debug-Modus: {(DEBUG_MODE ? "AKTIV" : "INAKTIV")}");
    System.Diagnostics.Debug.WriteLine($"📁 WebRoot: {app.Environment.WebRootPath}");
    System.Diagnostics.Debug.WriteLine($"🔗 API: /api/wochennachweis/");
    System.Diagnostics.Debug.WriteLine($"🎃 Feiertage-API: https://date.nager.at/");
    System.Diagnostics.Debug.WriteLine($"💻 Client-seitige Generierung: AKTIV");
    System.Diagnostics.Debug.WriteLine("===============================================");

    // Zusätzlich auch im Output-Fenster für bessere Sichtbarkeit
    Console.WriteLine("🚀 Debug-Modus ist AKTIV - Siehe VS Debug Console für Details");
}

app.Run();

// ================================
// 🔧 DEBUG-KONFIGURATION
// ================================
public class DebugOptions
{
    public bool EnableDebug { get; set; } = false;
    public bool EnableFormDebug { get; set; } = false;
    public bool EnableControllerDebug { get; set; } = false;
    public bool EnableJavaScriptDebug { get; set; } = false;
}

// ================================
// 🔧 DEBUG-SERVICE
// ================================
public class DebugService
{
    private readonly DebugOptions _options;

    public DebugService(Microsoft.Extensions.Options.IOptions<DebugOptions> options)
    {
        _options = options.Value;
    }

    public bool IsDebugEnabled => _options.EnableDebug;
    public bool IsFormDebugEnabled => _options.EnableFormDebug;
    public bool IsControllerDebugEnabled => _options.EnableControllerDebug;
    public bool IsJavaScriptDebugEnabled => _options.EnableJavaScriptDebug;

    public void LogDebug(string message, string category = "GENERAL")
    {
        if (_options.EnableDebug)
        {
            // WICHTIG: Nur System.Diagnostics.Debug.WriteLine geht zur VS Debug Console!
            System.Diagnostics.Debug.WriteLine($"🔧 [{category}] {message}");
        }
    }

    public void LogForm(string action, string details = "")
    {
        if (_options.EnableFormDebug)
        {
            System.Diagnostics.Debug.WriteLine($"📝 [FORM] {action} {details}");
        }
    }

    public void LogController(string controller, string action, string details = "")
    {
        if (_options.EnableControllerDebug)
        {
            System.Diagnostics.Debug.WriteLine($"🎯 [CONTROLLER] {controller}.{action} {details}");
        }
    }
}