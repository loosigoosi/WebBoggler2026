using Microsoft.AspNetCore.SignalR;
using WebBoggler.SignalRServer;
using WebBoggler.SignalRServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalR();

// Register RoomMaster as singleton
builder.Services.AddSingleton<RoomMaster>(serviceProvider =>
{
    var hubContext = serviceProvider.GetRequiredService<IHubContext<GameHub>>();
    return new RoomMaster(hubContext);
});

// Add CORS for OpenSilver client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:55591",  // IIS Express
                "http://localhost:55592",  // WebBoggler.Browser project
                "http://localhost:8733")   // IIS local
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize RoomMaster to start game logic
var roomMaster = app.Services.GetRequiredService<RoomMaster>();

// Configure middleware
app.UseCors();

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "WebBoggler SignalR Server is running. Connect to /gamehub");

app.Run();
