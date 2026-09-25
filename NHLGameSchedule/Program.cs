using NHLGameSchedule.Components;
using NHLGameSchedule.Models;
using NHLGameSchedule.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://0.0.0.0:10099");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("NHLGameSchedule");

builder.Services.AddScoped<TeamSelectionState>();
builder.Services.Configure<NhlScheduleOptions>(
    builder.Configuration.GetSection("NhlSchedule"));

builder.Services.AddHttpClient<INhlScheduleService, NhlScheduleService>(client =>
{
    client.BaseAddress = new Uri("https://api-web.nhle.com/v1/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (builder.Configuration.GetValue<bool>("EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.UseWebSockets();
app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
