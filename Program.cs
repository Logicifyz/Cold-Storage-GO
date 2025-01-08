using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // For Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Services;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddAuthorization();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Add your frontend URL
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// ✅ Proper Middleware Order
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Register Controllers in Pipeline
app.MapControllers();

// ✅ Enable Swagger Only for Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend"); // Ensure this comes before the custom middleware

app.UseMiddleware<SessionMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
