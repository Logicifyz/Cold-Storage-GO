using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models;
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Allow React app
              .AllowAnyHeader()                   // Allow any headers
              .AllowAnyMethod()                   // Allow any HTTP methods
              .AllowCredentials();                // Allow cookies/credentials
    });
});

// Register controllers explicitly
builder.Services.AddControllers();

// Stripe settings (optional, based on your project)
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Register services
builder.Services.AddDbContext<DbContexts>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Logging.AddConsole();

// Swagger configuration with security definition for session tokens
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

// Authentication setup
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Middleware pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp"); // CORS must be placed before authentication & controllers
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionMiddleware>();
app.MapControllers();
app.Run();
