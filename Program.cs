using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MySqlX.XDevAPI;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using System.Collections.Concurrent;
using System.Text.Json;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var clientId = builder.Configuration["OAuth:ClientId"];
var clientSecret = builder.Configuration["OAuth:ClientSecret"];
// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Register Controllers explicitly
// Add services to the container.
builder.Services.AddControllers();

// ✅ Register DbContext for MySQL (Ensure Proper Setup)
builder.Services.AddDbContext<DbContexts>();

// ✅ Register Services
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DeliveryService>();

// ✅ Swagger Setup
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<GoogleAuthService>();
builder.Services.AddLogging();
builder.Services.AddSingleton<WebSocketService>();

// Register WebSocketManager as a scoped service
builder.Services.AddScoped<WebSocketManager>();
// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("SessionId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Session ID for session validation",
        Name = "SessionId",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "SessionId"
                }
            },
            new string[] {}
        }
    });
});

// Add authentication and authorization
// ✅ Authentication and Authorization Setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = clientId;  // Now using configuration
    options.ClientSecret = clientSecret;  // Now using configuration
    options.CallbackPath = new PathString("/signin-google");
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<WebSocketManager>>();

app.UseWebSockets();
app.Map("/chat", chatApp =>
{
    chatApp.Use(async (context, next) =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Create a scope to resolve WebSocketManager
            using (var scope = context.RequestServices.CreateScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<WebSocketManager>();
                await HandleWebSocketConnection(webSocket, context, manager, context.RequestAborted);
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket request expected");
        }

        await next();
    });
});


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp"); // CORS middleware must come before Authorization
app.UseAuthentication();      // Add this if you are using authentication
app.UseAuthorization();
app.UseMiddleware<SessionMiddleware>();
app.MapControllers();
app.Run();

// Make the method non-static
async Task HandleWebSocketConnection(WebSocket webSocket, HttpContext httpContext, WebSocketManager manager, CancellationToken cancellationToken)
{
    // Extract the ticketId from the WebSocket request query string
    var ticketId = httpContext.Request.Query["ticketId"].ToString();
    if (string.IsNullOrEmpty(ticketId))
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<WebSocketManager>>();
        logger.LogError("TicketId is missing in the WebSocket request.");
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "TicketId is required", cancellationToken);
        return;
    }

    await manager.AddSocketAsync(webSocket, ticketId);

    var buffer = new byte[1024 * 4];
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await manager.RemoveSocketAsync(webSocket);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationToken);
            break;
        }
        else
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                var messageObject = JsonSerializer.Deserialize<WebSocketMessage>(message);
                if (messageObject != null)
                {
                    await manager.HandleMessageAsync(messageObject);

                    if (!string.IsNullOrEmpty(messageObject.ticketId))
                    {
                        await manager.SendChatHistoryAsync(messageObject.ticketId, webSocket);
                    }
                }
            }
            catch (JsonException ex)
            {
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<WebSocketManager>>();
                logger.LogError("Error parsing WebSocket message: {error}", ex.Message);
            }
        }
    }
}



public class WebSocketManager
{
    private readonly DbContexts _dbContext;
    private readonly ILogger<WebSocketManager> _logger;
    private readonly WebSocketService _webSocketService;

    public WebSocketManager(DbContexts dbContext, ILogger<WebSocketManager> logger, WebSocketService webSocketService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _webSocketService = webSocketService;
    }

    public async Task AddSocketAsync(WebSocket socket, string ticketId)
    {
        if (!_webSocketService.Sockets.ContainsKey(ticketId))
        {
            _webSocketService.Sockets[ticketId] = new List<WebSocket>();
        }

        if (!_webSocketService.Sockets[ticketId].Contains(socket))
        {
            _webSocketService.Sockets[ticketId].Add(socket);
            _logger.LogInformation("WebSocket added for TicketId={ticketId}", ticketId);
        }
        else
        {
            _logger.LogInformation("Socket already added for TicketId={ticketId}", ticketId);
        }
    }

    public async Task RemoveSocketAsync(WebSocket socket)
    {
        foreach (var entry in _webSocketService.Sockets)
        {
            if (entry.Value.Contains(socket))
            {
                entry.Value.Remove(socket);
                _logger.LogInformation("WebSocket removed for TicketId={ticketId}, RemainingSockets={count}", entry.Key, entry.Value.Count);

                if (!entry.Value.Any())
                {
                    _webSocketService.Sockets.TryRemove(entry.Key, out _);
                    _logger.LogInformation("All sockets removed for TicketId={ticketId}, Cleaning up", entry.Key);
                }
                break;
            }
        }
    }

    public async Task HandleMessageAsync(WebSocketMessage message)
    {
        LogSockets();

        if (!Guid.TryParse(message.senderId, out var senderId))
        {
            _logger.LogError("Invalid senderId format: {senderId}", message.senderId);
            return;
        }

        if (!Guid.TryParse(message.ticketId, out var ticketId))
        {
            _logger.LogError("Invalid ticketId format: {ticketId}", message.ticketId);
            return;
        }

        var chatMessage = new ChatMessage
        {
            Message = message.message,
            SentAt = DateTime.UtcNow,
            UserId = senderId,
            StaffId = message.staffId,
            TicketId = ticketId,
            IsStaffMessage = message.isStaff
        };

        _dbContext.ChatMessages.Add(chatMessage);
        await _dbContext.SaveChangesAsync();

        await BroadcastMessageAsync(message);
    }

    public async Task BroadcastMessageAsync(WebSocketMessage message)
    {
        LogSockets();
        message.type = "ReceiveMessage";

        _logger.LogInformation("Broadcasting message: {message}", JsonSerializer.Serialize(message));


        if (_webSocketService.Sockets.TryGetValue(message.ticketId, out var sockets))
        {
            _logger.LogInformation("Broadcasting message to {socketCount} sockets for TicketId={ticketId}", sockets.Count, message.ticketId);

            var jsonMessage = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(jsonMessage);

            foreach (var socket in sockets)
            {
                _logger.LogInformation("Attempting to send message to Socket: {socketHash}", socket.GetHashCode());

                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        _logger.LogInformation("Message sent successfully to Socket: {socketHash}", socket.GetHashCode());
                    }
                    else
                    {
                        _logger.LogWarning("Socket {socketHash} is not open, skipping send.", socket.GetHashCode());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to Socket: {socketHash}", socket.GetHashCode());
                }
            }
        }
        else
        {
            _logger.LogWarning("No sockets found for TicketId={ticketId} to broadcast the message.", message.ticketId);
        }
    }


    public async Task SendChatHistoryAsync(string ticketId, WebSocket socket)
    {
        LogSockets();
        var chatMessages = await _dbContext.ChatMessages
            .Where(m => m.TicketId.ToString() == ticketId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        var chatHistory = chatMessages.Select(message => new WebSocketMessage
        {
            senderId = message.UserId.ToString(),
            ticketId = message.TicketId.ToString(),
            message = message.Message,
            isStaff = message.IsStaffMessage,
            staffId = message.StaffId,
            type = "history"
        }).ToList();

        var webSocketHistoryMessage = new WebSocketMessage
        {
            type = "history",
            history = chatHistory
        };

        if (socket.State == WebSocketState.Open)
        {
            var jsonMessage = JsonSerializer.Serialize(webSocketHistoryMessage);
            var buffer = Encoding.UTF8.GetBytes(jsonMessage);
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    public void LogSockets()
    {
        foreach (var entry in _webSocketService.Sockets)
        {
            string userId = entry.Key;
            List<WebSocket> sockets = entry.Value;

            _logger.LogInformation($"User ID: {userId} has {sockets.Count} socket(s).");

            foreach (var socket in sockets)
            {
                _logger.LogInformation($"  Socket: {socket.GetHashCode()}");
            }
        }
    }
}

  






public class WebSocketMessage
{
    public string senderId { get; set; }
    public string ticketId { get; set; }
    public string message { get; set; }
    public bool isStaff { get; set; }
    public Guid staffId { get; set; } // Add staffId property
    public string type { get; set; }

    // Add the history property
    public List<WebSocketMessage> history { get; set; }
}


public class WebSocketService
{
    public ConcurrentDictionary<string, List<WebSocket>> Sockets { get; } = new();
}
