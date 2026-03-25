using EnoseLogger.Components;
using EnoseLogger.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Controllers
builder.Services.AddControllers();

// Configure port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5003); // Port 5003 for Enose Logger
});

// Register services
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<SessionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // Add controller endpoints
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auto-connect to MQTT broker on startup
var mqtt = app.Services.GetRequiredService<MqttService>();
var config = builder.Configuration.GetSection("MqttConfiguration");
var broker = config["BrokerHost"] ?? "139.162.62.210";
var port = int.Parse(config["BrokerPort"] ?? "1883");
var clientId = config["ClientId"] ?? "EnoseLogger_Net9";

await mqtt.ConnectAsync(broker, port, clientId);

app.Run();
