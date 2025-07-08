using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CookbookAppBackend.Data;
using CookbookAppBackend.Models;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Enable CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // Next.js default development port
                "http://localhost:8080",  // Your existing frontend URL
                "https://localhost:3000", // HTTPS version
                "https://localhost:8080"  // HTTPS version
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow credentials for authentication
    });
});

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add the required services for MVC/Web API controllers
builder.Services.AddControllers();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow HTTP for development
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero // Remove delay of token when expire
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

// Setup Swagger for API documentation with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cookbook API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cookbook API v1");
    });

    // Disable HTTPS redirection in development to avoid SSL issues
    // app.UseHttpsRedirection(); // Comment this out for now
}
else
{
    app.UseHttpsRedirection();
}

// Use middleware (order matters!)
app.UseCors("AllowFrontend");  // Enable CORS middleware for frontend

// IMPORTANT: Enable static files serving BEFORE authentication
app.UseStaticFiles();

// Configure static file serving for forum images
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "forum-images");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Add specific static file middleware for forum images
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/forum-images"
});

app.UseAuthentication(); // Enable Authentication middleware
app.UseAuthorization();  // Enable Authorization middleware

// Map controllers to routes
app.MapControllers();

// Ensure wwwroot folder exists for static files
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
}

var forumImagesPath = Path.Combine(webRootPath, "forum-images");
if (!Directory.Exists(forumImagesPath))
{
    Directory.CreateDirectory(forumImagesPath);
}

// Auto-migrate database on startup (optional - for development)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Only apply pending migrations if any exist
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        // Log the error - in production you'd want proper logging
        Console.WriteLine($"Migration error: {ex.Message}");
    }
}

app.Run();