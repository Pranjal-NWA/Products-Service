using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using ProductsService.Data;
using ProductsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    using var secretsClient = new Amazon.SecretsManager.AmazonSecretsManagerClient();
    var secretName = builder.Configuration["AWS:SecretName"] ?? "products-service/prod";
    var response = await secretsClient.GetSecretValueAsync(
        new Amazon.SecretsManager.Model.GetSecretValueRequest { SecretId = secretName });

    // Same shape as the Auth service's secret - a flat JSON blob
    // matching appsettings.json's own structure, e.g.
    // {"ConnectionStrings":{"DefaultConnection":"..."},"Jwt":{"SecretKey":"..."}}
    // Jwt:SecretKey's VALUE must match Auth's secret exactly, even
    // though this is a separate, independently-named AWS secret.
    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.SecretString));
    builder.Configuration.AddJsonStream(stream);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string is not configured.");

var jwtSecret = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT secret key is not configured.");

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Hide fields that don't apply to a product (e.g. blades on a motor)
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IImageStorage, LocalImageStorage>();

// No AddIdentityCore, no UserManager, no SignInManager, no
// AddDataProtection - this service never creates accounts or checks
// passwords, it only validates tokens Auth already issued.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },

            // Accept the role whether the token carries it as "role" or as
            // the long ClaimTypes.Role URI, so [Authorize(Roles = "Admin")] works either way.
            OnTokenValidated = context =>
            {
                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                foreach (var c in identity.FindAll("role").ToList())
                {
                    if (!identity.HasClaim(ClaimTypes.Role, c.Value))
                        identity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();   // dev runs plain http (phone / Vite proxy)
}

// Serve uploaded images from the local folder (before auth so they stay public)
var uploadRoot = app.Configuration["Images:LocalPath"]
    ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "products");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads/products",
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/healthz", async (ProductsDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ok", db = "ok" });
    }
    catch
    {
        return Results.Json(new { status = "error", db = "unreachable" }, statusCode: 503);
    }
}).AllowAnonymous();

app.Run();

public partial class Program { }