using System.Text;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Data.Seed;
using ClaimFlow.Api.Security;
using ClaimFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ClaimFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClaimFlow")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<ClaimRulesOptions>(builder.Configuration.GetSection(ClaimRulesOptions.SectionName));
builder.Services.Configure<DemoAdminOptions>(builder.Configuration.GetSection(DemoAdminOptions.SectionName));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ClaimAdjudicationService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClaimFlowDbContext>();
    await db.Database.MigrateAsync();

    var demoAdminOptions = scope.ServiceProvider.GetRequiredService<IOptions<DemoAdminOptions>>();
    await DemoDataSeeder.SeedAsync(db, demoAdminOptions, DateTime.UtcNow);
}

app.Run();

public partial class Program;
