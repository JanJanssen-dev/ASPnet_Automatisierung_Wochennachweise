#nullable disable

using ASPnet_Automatisierung_Wochennachweise.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// API Controller Support hinzufügen
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // JSON-Serialisierung konfigurieren
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
        // Deutsche Datumsformate unterstützen
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Session für Zeiträume-Management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// CORS für lokale Entwicklung und Client-seitige API-Calls
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// HttpClient für Feiertag-API registrieren
builder.Services.AddHttpClient<FeiertagService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "WochennachweisGenerator/1.0");
});

// Services registrieren - NUR noch die, die wir brauchen
builder.Services.AddSingleton<FeiertagService>();
builder.Services.AddScoped<WochennachweisGenerator>();
// DocumentService entfernt - nicht mehr benötigt für Client-seitige Generierung

// Logging konfigurieren
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // CORS nur in Development
    app.UseCors("AllowLocalhost");
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// API Routes zuerst (wichtig für die Reihenfolge)
app.MapControllers();

// Standard MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Startup-Informationen loggen
if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("🚀 Wochennachweis-Generator gestartet (Client-seitige Generierung)");
    app.Logger.LogInformation("📁 wwwroot Pfad: {WebRootPath}", app.Environment.WebRootPath);
    app.Logger.LogInformation("🔗 API verfügbar unter: /api/wochennachweis/");
    app.Logger.LogInformation("🎃 Feiertage-API: https://date.nager.at/api/v3/publicholidays/");
    app.Logger.LogInformation("💻 Client-seitige Dokumenterstellung aktiv");
}

app.Run();