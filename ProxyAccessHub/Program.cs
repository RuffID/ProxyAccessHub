using ProxyAccessHub.Core;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Configuration;
using ProxyAccessHub.Infrastructure.Service.DataBase;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string configPath = ConfigPathResolver.Resolve(builder.Environment.EnvironmentName, builder.Environment.ContentRootPath);

builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: false);

Log.Logger = new LoggerConfiguration()
    .Enrich.With(new SimpleClassNameEnricher())
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
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

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.Run();
