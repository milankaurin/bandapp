using BandApplicationBack.Domain;
using BandApplicationBack.Endpoints;
using BandApplicationBack.Infrastructure;
using BandApplicationBack.Infrastructure.Hubs;
using BandApplicationBack.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF
builder.Services.AddDbContext<BandAppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.CommandTimeout(60);        // èeka do 60s (serverless wake-up)
            sql.EnableRetryOnFailure();    // retry za Azure SQL
        }));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – dozvoli SPA koji radi na 5001 (i dev varijante po želji)
var corsSection = builder.Configuration.GetSection("Cors");
var corsPolicyName = corsSection.GetValue<string>("PolicyName");
var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
var allowCredentials = corsSection.GetValue<bool>("AllowCredentials");

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        corsPolicyName!,
        policy =>
        {
            policy.WithOrigins(allowedOrigins!).AllowAnyMethod().AllowAnyHeader();

            if (allowCredentials)
                policy.AllowCredentials();
        }
    );
});

// Repo + SignalR
builder.Services.AddScoped<SongRepository>();
builder.Services.AddScoped<ArtistRepository>();
builder.Services.AddSignalR();

var app = builder.Build();

// Swagger – omoguæi i u Production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // relativan endpoint da radi i ako je app pod virtualnom putanjom
    c.SwaggerEndpoint("v1/swagger.json", "BandApplicationBack v1");
    c.RoutePrefix = "swagger"; // => /swagger
});

// HTTPS redirekcija (ok i u IIS InProcess)
app.UseHttpsRedirection();
app.UseRouting();

// CORS MORA da bude PRE mapiranja ruta/hubova
app.UseCors("AllowBlazorClient");

// Minimal API endpoints + SignalR (uz CORS)
app.MapHub<BandAppHub>("/songHub").RequireCors("AllowBlazorClient");

app.MapSongEndpoints();
app.MapArtistEndpoints();
app.MapSessionEndpoints();

app.Run();
