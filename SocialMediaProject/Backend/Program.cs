using System.Text;
using Backend.Data;
using Backend.Configuration;
using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Azure.Core;



var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AuthCookieOptions>(
    builder.Configuration.GetSection("AuthCookie"));

// === PHASE 1: REGISTER SERVICES (Must be BEFORE builder.Build()) ===
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Backend API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token. Swagger will add the 'Bearer ' prefix."
    });
});
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var namespaceName = configuration["ServiceBus:Namespace"]!;

    TokenCredential credential;

    if (builder.Environment.IsDevelopment())
    {
        credential = new AzureCliCredential();
    }
    else
    {
        credential = new ManagedIdentityCredential();
    }

    return new ServiceBusClient(namespaceName, credential);
});
builder.Services.AddHostedService<ServicebusReceiver>();
builder.Services.AddSingleton<ServiceBusPublisher>();
builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddScoped<IPostService,PostService>();
builder.Services.AddScoped<IReplyService,ReplyService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IProfileService,ProfileService>();
builder.Services.AddScoped<IMessageService,MessageService>();
builder.Services.AddScoped<INotificationService,NotificationService>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddSingleton<IPresenceService,PresenceService>();
//builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        var frontendOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // 2. Define the exact rules for what makes a token "valid"
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, // Force the system to check our signature
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretBackEndKeyPolymedicure123!")), // Our secret key
        ValidateIssuer = false,   // Set to true if you want to verify the specific server that generated it
        ValidateAudience = false, // Set to true if you want to verify a specific frontend application URL
        RequireExpirationTime = true,
        ValidateLifetime = true,  // Force expiration validation checks
        ClockSkew = TimeSpan.Zero // Removes the default 5-minute grace period buffer
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
// Moved up here! Now .NET knows about the database before building the app
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================================================================

var app = builder.Build();

// === PHASE 2: CONFIGURE PIPELINE (Must be AFTER builder.Build()) ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnablePersistAuthorization();
    });
}
app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthentication(); // 💂‍♂️ Guard 1: Who are you? (Extracts and validates the JWT)
app.UseAuthorization();  // 💂‍♂️ Guard 2: Are you allowed in? (Checks endpoint permissions)
app.UseStaticFiles();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapGet("/", () => "Rhino backend is running!");

app.Run();