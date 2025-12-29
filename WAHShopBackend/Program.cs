using WAHShopBackend.Data;
using WAHShopBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WAHShopBackend.EmailF;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using WAHShopBackend.ProductP;
using WAHShopBackend.ImagesF;
using Microsoft.AspNetCore.HttpOverrides;
using WAHShopBackend.TelegramF;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.  
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// project info
builder.Services.Configure<ProjectInfo>(
    builder.Configuration.GetSection("ProjectInfo"));
//  
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
builder.Services.AddAuthorization();
// token validation parameters
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key!))
    };
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId =
        builder.Configuration["Authentification:Google:ClientId"] ?? "";

    googleOptions.ClientSecret =
        builder.Configuration["Authentification:Google:ClientSecret"] ?? "";
}); 

// dataabse
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));
// email service 
builder.Services.AddScoped<EmailService>();
// product service
builder.Services.AddScoped<ProductService>();
// productimages service
builder.Services.AddScoped<ProductImagesService>();
// CarouselImages service
builder.Services.AddScoped<CarouselImagesService>();
// telegram service
builder.Services.Configure<TelegramBotSettings>(
    builder.Configuration.GetSection("TelegramBot"));

builder.Services.AddSingleton<TelegramBotClient>(sp =>
{
    // Einstellungen abrufen
    var settings = sp.GetRequiredService<IOptions<TelegramBotSettings>>().Value;
    return new TelegramBotClient(settings.Token); // bot ersteööem
});

builder.Services.AddSingleton<TelegramService>();
// email token
builder.Services.AddIdentity<UserIdentity, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyDbContext>()
.AddDefaultTokenProviders();

// app config
var appConfig = builder.Configuration.GetSection("AppConfig").Get<AppConfig>() ?? new AppConfig();

if (builder.Environment.IsDevelopment())
{
    appConfig.ShareStoragePath = Path.Combine(builder.Environment.ContentRootPath, "ShareStorage");
}
else
{
    appConfig.ShareStoragePath = @"C:\inetpub\ShareStorage";
}
// Speichern Sie die formatierte Version in DI.
builder.Services.AddSingleton(appConfig);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost
});

// static files für share storage
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(appConfig.ShareStoragePath),
    RequestPath = "/api/ShareStorage"
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(appConfig.ShareStoragePath),
    RequestPath = "/api/ShareStorage"
});


// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// server 
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
