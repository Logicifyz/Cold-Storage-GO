using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;

var builder = WebApplication.CreateBuilder(args);

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

// ✅ Authentication Setup
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

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
