using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;


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
