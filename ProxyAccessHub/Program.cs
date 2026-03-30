using ProxyAccessHub.Core;

var builder = WebApplication.CreateBuilder(args);
var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "config.json");

builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);

builder.Services.ConfigureServices(builder);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
