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
                "http://localhost:8733",   // IIS local
                "http://webboggler.xidea.it:80") // WebBoggler hosted online

              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize RoomMaster to start game logic
var roomMaster = app.Services.GetRequiredService<RoomMaster>();
var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();

// Subscribe to RoomMaster events to send SignalR messages
roomMaster.RoundStart += async () =>
{
    Console.WriteLine("[Program.RoundStart] Event fired, sending StartRound to all clients...");
    await hubContext.Clients.All.SendAsync("StartRound");
    Console.WriteLine("[Program.RoundStart] StartRound sent successfully");
};

roomMaster.RoundTerminate += async () =>
{
    Console.WriteLine("[Program.RoundTerminate] Event fired, sending EndRound to all clients...");
    await hubContext.Clients.All.SendAsync("EndRound");
    Console.WriteLine("[Program.RoundTerminate] EndRound sent successfully");
};

roomMaster.ValidatedResults += async () =>
{
    Console.WriteLine("[Program.ValidatedResults] Event fired, sending ShowTime to all clients...");
    try
    {
        await hubContext.Clients.All.SendAsync("ShowTime");
        Console.WriteLine("[Program.ValidatedResults] ShowTime sent successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Program.ValidatedResults] ERROR sending ShowTime: {ex.Message}");
    }
};

roomMaster.NewMatchKeepReady += async () =>
{
    Console.WriteLine("[Program.NewMatchKeepReady] Event fired, sending BoardServed to all clients...");
    await hubContext.Clients.All.SendAsync("BoardServed");
    Console.WriteLine("[Program.NewMatchKeepReady] BoardServed sent successfully");
};

roomMaster.ScoreChange += async () =>
{
    Console.WriteLine("[Program.ScoreChange] Event fired, sending UpdatePlayers to all clients...");
    await hubContext.Clients.All.SendAsync("UpdatePlayers");
    Console.WriteLine("[Program.ScoreChange] UpdatePlayers sent successfully");
};

roomMaster.BoardDiscarded += async () =>
{
    Console.WriteLine("[Program.BoardDiscarded] Event fired, board was discarded by all players");
    // Il NewMatchKeepReady verrà chiamato subito dopo da CheckDiscard
    // Questo evento può essere usato per statistiche o log
};

// Configure middleware
app.UseCors();

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "WebBoggler SignalR Server is running. Connect to /gamehub");

app.Run();
