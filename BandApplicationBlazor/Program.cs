using BandApplicationBlazor;
using BandApplicationFront.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5097") });

//builder.Services.AddScoped(sp => new HttpClient
//{
//    BaseAddress = new Uri("https://192.168.100.8:443"),
//});

var apiBaseUrl =
    builder.Configuration["Api:BaseUrl"] ?? throw new Exception("Api:BaseUrl nije definisan");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;

    config.SnackbarConfiguration.VisibleStateDuration = 2000; // ⬅ 2 sekunde
});
builder.Services.AddScoped<SongService>();

await builder.Build().RunAsync();
