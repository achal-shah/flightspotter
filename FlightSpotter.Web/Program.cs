using Azure.Identity;
using FlightSpotter.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Validators;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var keyVaultUrl = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault( new Uri(keyVaultUrl), new DefaultAzureCredential());
}

// Authentication: use OpenID Connect (Microsoft identity platform v2.0) with cookie auth as the local scheme.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        // Use the v2.0 common endpoint to allow both work/school and personal Microsoft accounts
        options.Authority = "https://login.microsoftonline.com/common/v2.0";
        options.ClientId = cfg["AzureAd:ClientId"];
        options.ClientSecret = cfg["AzureAd:ClientSecret"];
        options.CallbackPath = "/signin-oidc";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("offline_access");
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.TokenValidationParameters.IssuerValidator =
            AadIssuerValidator.GetAadIssuerValidator(options.Authority).Validate;
    });

// Require authentication by default for all requests (only authenticated MSA users can access)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Flights}/{action=Index}/{id?}");

app.Run();
