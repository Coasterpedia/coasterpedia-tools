using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CoasterpediaTools.Authentication;
using CoasterpediaTools.Authentication.Handler;
using CoasterpediaTools.Authentication.Scheme;
using CoasterpediaTools.Clients.Wiki;
using CoasterpediaTools.Components;
using CoasterpediaTools.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var coasterpediaConfig = builder.Configuration.GetRequiredSection(nameof(CoasterpediaConfig)).Get<CoasterpediaConfig>();
var oauthConfig = builder.Configuration.GetRequiredSection(nameof(OAuthConfig)).Get<OAuthConfig>();
builder.Services.AddOptions<CoasterpediaConfig>();
builder.Services.AddOptions<OAuthConfig>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = MediawikiAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = MediawikiAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/signin";
        options.LogoutPath = "/signout";
        options.EventsType = typeof(CookieEvents);
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(28);
        options.Cookie.Name = "User";
        options.Cookie.MaxAge = TimeSpan.FromDays(180);
    })
    .AddMediawiki(options =>
    {
        options.ClientId = oauthConfig.ClientId;
        options.ClientSecret = oauthConfig.ClientSecret;
        // options.AuthorizationEndpoint = coasterpediaConfig.BaseUrl + "/w/rest.php/oauth2/authorize";
        options.TokenEndpoint = coasterpediaConfig.BaseUrl + "/w/rest.php/oauth2/access_token";
        options.UserInformationEndpoint = coasterpediaConfig.BaseUrl + "/w/rest.php/oauth2/resource/profile";
        // options.SaveTokens = true;
    });

builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddRefitClient<IRefreshTokenClient>(new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        })
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(coasterpediaConfig.BaseUrl + "/w/rest.php");
        c.DefaultRequestHeaders.UserAgent.ParseAdd("CoasterpediaTools/1.0");
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{oauthConfig.ClientId}:{oauthConfig.ClientSecret}")));
    });

builder.Services.AddHybridCache();
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMySqlCache(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.SchemaName = "coasterpedia_tools";
        options.TableName = "Cache";
    });
}

builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped<TokenHandler>();

builder.Services.AddCircuitServicesAccessor();

builder.Services.AddHttpClient<CoasterpediaClient>(config =>
    {
        config.BaseAddress = new Uri(coasterpediaConfig.BaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    })
    .ConfigureHttpClient(c => { c.DefaultRequestHeaders.UserAgent.ParseAdd("CoasterpediaTools/1.0"); })
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<CommonsClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    })
    .ConfigureHttpClient(c => { c.DefaultRequestHeaders.UserAgent.ParseAdd("CoasterpediaTools/1.0 (https://coasterpedia.net)"); });

builder.Services.AddSingleton<WikiSiteAccessor>();

builder.Services.AddTransient<CookieEvents>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(next => context => {
    if (string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase)) {
        context.Request.Scheme = "https";
    }

    return next(context);
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRouting();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapDefaultControllerRoute();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();