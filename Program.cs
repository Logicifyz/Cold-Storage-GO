using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models;
using Cold_Storage_GO;
using Cold_Storage_GO.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register Controllers explicitly
builder.Services.AddControllers();

// ✅ Register DbContext for MySQL (Ensure Proper Setup)
builder.Services.AddDbContext<DbContexts>();

// ✅ Register Services
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<DeliveryService>();

// ✅ Swagger Setup
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

// ✅ Authentication and Authorization Setup
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization();

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

app.Run();
