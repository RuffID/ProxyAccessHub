using ProxyAccessHub.Core;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Service.DataBase;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string configPath = Path.Combine(AppContext.BaseDirectory, "Config", "config.json");

builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);

builder.Services.ConfigureServices(builder);

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    DataBaseCheckUpService<ProxyAccessHubDbContext> dataBaseCheckUpService = scope.ServiceProvider.GetRequiredService<DataBaseCheckUpService<ProxyAccessHubDbContext>>();
    dataBaseCheckUpService.CheckOrUpdateDb();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
