using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add logging configuration
builder.Logging.ClearProviders(); // Clear default providers
builder.Logging.AddConsole();    // Add console logging
builder.Logging.AddDebug();      // Add debug logging for development

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

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true); // Load OpenAI key

// ✅ Register Controllers explicitly
// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Ensure normal naming
    });


// ✅ Register DbContext for MySQL (Ensure Proper Setup)
builder.Services.AddDbContext<DbContexts>();

// ✅ Register Services
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DeliveryService>();

// ✅ Swagger Setup
builder.Services.AddSingleton<EmailService>();

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
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Add logging for the startup phase
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application is starting...");

app.UseHttpsRedirection();

// Serve static files from the "MediaFiles" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "MediaFiles")),
    RequestPath = "/MediaFiles"
});

app.UseCors("AllowReactApp"); // CORS middleware must come before Authorization
app.UseAuthentication();      // Add this if you are using authentication
app.UseAuthorization();
app.UseMiddleware<SessionMiddleware>();
app.MapControllers();

logger.LogInformation("Application configuration completed. Ready to handle requests.");
app.Run();
