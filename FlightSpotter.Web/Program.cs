using FlightSpotter.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<FlightTableService>(sp =>
    new FlightTableService(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IHostEnvironment>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Flights}/{action=Index}/{id?}");

app.Run();
