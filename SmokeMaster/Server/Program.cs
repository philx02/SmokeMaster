using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton<SmokeMaster.Server.Hubs.SmokerDataService>();
#pragma warning disable CS8603 // Possible null reference return.
builder.Services.AddHostedService<SmokeMaster.Server.Hubs.SmokerDataService>(provider => provider.GetService<SmokeMaster.Server.Hubs.SmokerDataService>());
#pragma warning restore CS8603 // Possible null reference return.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseWebAssemblyDebugging();
}
else
{
  app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<SmokeMaster.Server.Hubs.UiHub>("/smoker_hub");

app.Run();
