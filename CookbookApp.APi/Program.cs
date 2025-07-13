using CookbookApp.APi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CookbookDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.EnableRetryOnFailure())
    .EnableDetailedErrors()
    .EnableSensitiveDataLogging());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var imagesPath = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "images");
if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Ensure the application is stopped before making changes to the source code.
// This avoids runtime errors like ENC0097.
// Ensure the application is stopped before making changes to the source code.
// This avoids runtime errors like ENC0097.

// The ENC0097 error is not caused by code, but by editing and applying changes while the app is running.
// To fix: Stop the application in Visual Studio before making code changes, then rebuild and run again.

// No code changes are required to fix ENC0097.
