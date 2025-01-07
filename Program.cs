using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.OpenApi.Models; // Make sure to include this for Swagger
using Cold_Storage_GO;
using Cold_Storage_GO.Middleware;
using Cold_Storage_GO.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<DbContexts>();
builder.Services.AddSingleton<EmailService>();

// Swagger configuration to include the SessionId header
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add the custom SessionId header to Swagger
    c.AddSecurityDefinition("SessionId", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Session ID for session validation",
        Name = "SessionId",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer"
    });

    // Add a security requirement to include the SessionId header in Swagger UI
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

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Use the custom SessionMiddleware for session validation
app.UseMiddleware<SessionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
