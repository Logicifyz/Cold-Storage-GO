using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models;
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ✅ Correct way to initialize Stripe without namespace conflicts
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));


// ✅ Register Services Explicitly to Prevent Conflicts
builder.Services.AddDbContext<DbContexts>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddSingleton<EmailService>();
builder.Logging.AddConsole();


// ✅ CORS Setup for React Frontend Integration
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

// ✅ Register Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<Cold_Storage_GO.Services.SubscriptionService>();

// ✅ Swagger Configuration with Security Definition for Session Tokens
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("SessionId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Session ID for security validation",
        Name = "SessionId",
        Type = SecuritySchemeType.ApiKey
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
            Array.Empty<string>()
        }
    });
});

// ✅ Authentication Setup
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization();

// ✅ Middleware Pipeline
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionMiddleware>();
app.MapControllers();
app.Run();
