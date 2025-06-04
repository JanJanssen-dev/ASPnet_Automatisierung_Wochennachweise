//#nullable disable

using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ================================
// 🔧 ERWEITERTE DEBUG-KONFIGURATION
// ================================
const bool DEBUG_MODE = true;

if (DEBUG_MODE)
{
    builder.Services.Configure<DebugOptions>(options =>
    {
        options.EnableDebug = true;
        options.EnableFormDebug = true;
        options.EnableControllerDebug = true;
        options.EnableJavaScriptDebug = true;
    });

    builder.Logging.ClearProviders();
    builder.Logging.AddDebug();
    builder.Logging.AddConsole();
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

// ================================
// 🔧 CORE SERVICES
// ================================
builder.Services.AddControllersWithViews();

// API Controller Support mit erweiterten JSON-Optionen
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ================================
// 🔧 SESSION MANAGEMENT (VERBESSERT)
// ================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Verlängert für bessere UX
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ================================
// 🔧 CORS FÜR ENTWICKLUNG
// ================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ================================
// 🔧 HTTP CLIENT FÜR FEIERTAG-API (ERWEITERT)
// ================================
builder.Services.AddHttpClient<FeiertagService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(15); // Erhöht für bessere Stabilität
    client.DefaultRequestHeaders.Add("User-Agent", "WochennachweisGenerator/2.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ================================
// 🔧 DEPENDENCY INJECTION
// ================================
builder.Services.AddSingleton<FeiertagService>();
builder.Services.AddScoped<WochennachweisGenerator>();
builder.Services.AddScoped<DebugService>();

// ================================
// 🔧 ZUSÄTZLICHE SERVICES
// ================================
// Memory Cache für bessere Performance
builder.Services.AddMemoryCache();

// Logging für besseres Debugging
builder.Services.AddLogging(logging =>
{
    if (DEBUG_MODE)
    {
        logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        logging.AddFilter("ASPnet_Automatisierung_Wochennachweise", LogLevel.Debug);
    }
});

var app = builder.Build();

// ================================
// 🔧 MIDDLEWARE PIPELINE
// ================================
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

// ================================
// 🔧 ROUTING UND SESSION
// ================================
app.UseRouting();
app.UseSession(); // Session vor Authorization!
app.UseAuthorization();

// ================================
// 🔧 ROUTE MAPPING
// ================================
// API Routes zuerst (wichtig für API-Endpoints)
app.MapControllers();

// Standard MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Zusätzliche spezifische Routes
app.MapControllerRoute(
    name: "templatehelp",
    pattern: "template-hilfe",
    defaults: new { controller = "Home", action = "TemplateHelp" });

app.MapControllerRoute(
    name: "api-test",
    pattern: "api-test",
    defaults: new { controller = "Wochennachweis", action = "Test" });

// ================================
// 🔧 STARTUP-LOGGING UND DIAGNOSE
// ================================
if (DEBUG_MODE)
{
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine("🚀 ERWEITERTE WOCHENNACHWEIS-GENERATOR VERSION 2.0");
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine($"🔧 Debug-Modus: {(DEBUG_MODE ? "AKTIV" : "INAKTIV")}");
    System.Diagnostics.Debug.WriteLine($"📁 WebRoot: {app.Environment.WebRootPath}");
    System.Diagnostics.Debug.WriteLine($"📁 ContentRoot: {app.Environment.ContentRootPath}");
    System.Diagnostics.Debug.WriteLine($"🌍 Environment: {app.Environment.EnvironmentName}");
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine("🔗 VERFÜGBARE ENDPOINTS:");
    System.Diagnostics.Debug.WriteLine("   • / - Hauptformular (Index)");
    System.Diagnostics.Debug.WriteLine("   • /template-hilfe - Template-Platzhalter");
    System.Diagnostics.Debug.WriteLine("   • /api/wochennachweis/test - API-Test");
    System.Diagnostics.Debug.WriteLine("   • /api/wochennachweis/template - Template-Download");
    System.Diagnostics.Debug.WriteLine("   • /api/wochennachweis/generate-data - Daten-Generierung");
    System.Diagnostics.Debug.WriteLine("   • /api/wochennachweis/feiertage/{year}?bundesland={code}");
    System.Diagnostics.Debug.WriteLine("   • /api/wochennachweis/bundeslaender - Bundesländer-Liste");
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine("🔥 NEUE FEATURES:");
    System.Diagnostics.Debug.WriteLine("   ✅ Tagesbasierte Wochenlogik");
    System.Diagnostics.Debug.WriteLine("   ✅ Bundesland-spezifische Feiertage");
    System.Diagnostics.Debug.WriteLine("   ✅ Überschneidungsvalidierung");
    System.Diagnostics.Debug.WriteLine("   ✅ ZIP-Unterordner (Praktikum/, Umschulung/)");
    System.Diagnostics.Debug.WriteLine("   ✅ Inline-Zeitraum-Eingabe (kein Modal)");
    System.Diagnostics.Debug.WriteLine("   ✅ Signatur-Upload (Base64)");
    System.Diagnostics.Debug.WriteLine("   ✅ Reset-Funktion");
    System.Diagnostics.Debug.WriteLine("   ✅ Template-Hilfe-Seite");
    System.Diagnostics.Debug.WriteLine("   ✅ Verbesserte Session-Verwaltung");
    System.Diagnostics.Debug.WriteLine("   ✅ Browser-Zurück-Button-Kompatibilität");
    System.Diagnostics.Debug.WriteLine("===============================================");
    System.Diagnostics.Debug.WriteLine("📊 TECHNISCHE DETAILS:");
    System.Diagnostics.Debug.WriteLine($"   • ASP.NET Core: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
    System.Diagnostics.Debug.WriteLine("   • Session-Timeout: 60 Minuten");
    System.Diagnostics.Debug.WriteLine("   • API-Timeout: 15 Sekunden");
    System.Diagnostics.Debug.WriteLine("   • Client-seitige Generierung: AKTIV");
    System.Diagnostics.Debug.WriteLine("   • Feiertags-API: Nager.at");
    System.Diagnostics.Debug.WriteLine("   • Template-Engine: Docxtemplater");
    System.Diagnostics.Debug.WriteLine("===============================================");

    // Template-Datei-Prüfung beim Start
    var templatePath = Path.Combine(app.Environment.WebRootPath, "templates", "Wochennachweis_Vorlage.docx");
    var templateExists = File.Exists(templatePath);
    var templateSize = templateExists ? new FileInfo(templatePath).Length : 0;

    System.Diagnostics.Debug.WriteLine("📄 TEMPLATE-DIAGNOSE:");
    System.Diagnostics.Debug.WriteLine($"   • Pfad: {templatePath}");
    System.Diagnostics.Debug.WriteLine($"   • Existiert: {(templateExists ? "✅ JA" : "❌ NEIN")}");
    if (templateExists)
    {
        System.Diagnostics.Debug.WriteLine($"   • Größe: {templateSize:N0} Bytes ({Math.Round(templateSize / 1024.0, 2)} KB)");
        System.Diagnostics.Debug.WriteLine($"   • Status: {(templateSize > 1000 ? "✅ OK" : "⚠️ VERDÄCHTIG KLEIN")}");
    }
    else
    {
        System.Diagnostics.Debug.WriteLine("   ⚠️ WARNUNG: Template-Datei nicht gefunden!");
        System.Diagnostics.Debug.WriteLine("   💡 Erstellen Sie: wwwroot/templates/Wochennachweis_Vorlage.docx");
    }
    System.Diagnostics.Debug.WriteLine("===============================================");

    // Zusätzlich im Output-Fenster für bessere Sichtbarkeit
    Console.WriteLine("🚀 Erweiterte Wochennachweis-Generator V2.0 ist AKTIV");
    Console.WriteLine($"🔧 Template: {(templateExists ? "✅ Gefunden" : "❌ Fehlt")}");
    Console.WriteLine("🔧 Siehe VS Debug Console für vollständige Details");
}

// ================================
// 🔧 GRACEFUL SHUTDOWN
// ================================
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    if (DEBUG_MODE)
    {
        System.Diagnostics.Debug.WriteLine("🛑 Wochennachweis-Generator wird beendet...");
    }
});

app.Run();

// ================================
// 🔧 ERWEITERTE DEBUG-KONFIGURATION
// ================================
public class DebugOptions
{
    public bool EnableDebug { get; set; } = false;
    public bool EnableFormDebug { get; set; } = false;
    public bool EnableControllerDebug { get; set; } = false;
    public bool EnableJavaScriptDebug { get; set; } = false;
}

// ================================
// 🔧 ERWEITERTE DEBUG-SERVICE
// ================================
public class DebugService
{
    private readonly DebugOptions _options;
    private readonly ILogger<DebugService> _logger;

    public DebugService(Microsoft.Extensions.Options.IOptions<DebugOptions> options, ILogger<DebugService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsDebugEnabled => _options.EnableDebug;
    public bool IsFormDebugEnabled => _options.EnableFormDebug;
    public bool IsControllerDebugEnabled => _options.EnableControllerDebug;
    public bool IsJavaScriptDebugEnabled => _options.EnableJavaScriptDebug;

    public void LogDebug(string message, string category = "GENERAL")
    {
        if (_options.EnableDebug)
        {
            // VS Debug Console (für Entwicklung)
            System.Diagnostics.Debug.WriteLine($"🔧 [{category}] {message}");

            // Zusätzlich über ILogger (für Logs)
            _logger.LogDebug("[{Category}] {Message}", category, message);
        }
    }

    public void LogForm(string action, string details = "")
    {
        if (_options.EnableFormDebug)
        {
            System.Diagnostics.Debug.WriteLine($"📝 [FORM] {action} {details}");
            _logger.LogDebug("[FORM] {Action} {Details}", action, details);
        }
    }

    public void LogController(string controller, string action, string details = "")
    {
        if (_options.EnableControllerDebug)
        {
            System.Diagnostics.Debug.WriteLine($"🎯 [CONTROLLER] {controller}.{action} {details}");
            _logger.LogDebug("[CONTROLLER] {Controller}.{Action} {Details}", controller, action, details);
        }
    }

    public void LogError(string message, Exception? exception = null)
    {
        System.Diagnostics.Debug.WriteLine($"❌ [ERROR] {message}");

        if (exception != null)
        {
            _logger.LogError(exception, "{Message}", message);
        }
        else
        {
            _logger.LogError("{Message}", message);
        }
    }

    public void LogPerformance(string operation, TimeSpan duration)
    {
        if (_options.EnableDebug)
        {
            System.Diagnostics.Debug.WriteLine($"⏱️ [PERFORMANCE] {operation}: {duration.TotalMilliseconds:F2}ms");
            _logger.LogDebug("[PERFORMANCE] {Operation}: {Duration}ms", operation, duration.TotalMilliseconds);
        }
    }
}