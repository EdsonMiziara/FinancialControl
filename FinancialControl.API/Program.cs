using FinancialControl.API.Interfaces;
using FinancialControl.API.Models;
using FinancialControl.API.Services;
using FinancialControl.Shared.CacheHolder;
using FinancialControl.Shared.Services;
using FinancialControl.Shared.SupportModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// CACHE + IA
builder.Services.AddSingleton<CategorizerCache>();
builder.Services.AddScoped<CategorizerLoader>();
builder.Services.AddSingleton<CategorizerService>();
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("SUA_CHAVE_SECRETA"))
        };
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Load cache on startup

using (var scope = app.Services.CreateScope())
{
    var loader = scope.ServiceProvider.GetRequiredService<CategorizerLoader>();
    var cache = scope.ServiceProvider.GetRequiredService<CategorizerCache>();

    var data = await loader.LoadAsync();

    cache.Categorias = data.Categorias;
    cache.Aprendizados = data.Aprendizados;
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();