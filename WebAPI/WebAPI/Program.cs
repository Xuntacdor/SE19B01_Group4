using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI;
using Stripe;
using System.ClientModel;
using WebAPI.Authorization;
using WebAPI.Data;
using WebAPI.ExternalServices;
using WebAPI.Repositories;
using WebAPI.Services;
using WebAPI.Services.Authorization;
using WebAPI.Services.Payments;
using WebAPI.Services.Webhooks;
//using WebAPI.Services.Payments;
//using WebAPI.Services.Webhooks;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var secretsJson = Environment.GetEnvironmentVariable("IELTSWebSecret");
if (!string.IsNullOrWhiteSpace(secretsJson))
{
    try {

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(secretsJson));
        builder.Configuration.AddJsonStream(stream);
        Console.WriteLine("[CONFIG] Loaded secrets from env IELTSWebSecret (JSON)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to load IELTSWebSecret JSON: {ex.Message}");
    }
}
// ======================================
// Controllers & JSON config
// ======================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AiOptions"));

builder.Services.AddSingleton(sp =>
{
    var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>().Value;
    var apiKey = builder.Configuration["OpenAI:ApiKey"];

    OpenAIClient client;

    if (aiOptions.Provider?.Equals("Local", StringComparison.OrdinalIgnoreCase) == true)
    {
        var opts = new OpenAIClientOptions
        {
            Endpoint = new Uri(aiOptions.BaseUrl ?? "http://localhost:1234/v1")
        };
        client = new OpenAIClient(new ApiKeyCredential("dummy-key"), opts);
        Console.WriteLine($"[AI] Using LOCAL LM Studio at {opts.Endpoint}");
    }
    else
    {
        client = new OpenAIClient(new ApiKeyCredential(apiKey));
        Console.WriteLine("[AI] Using CLOUD OpenAI API");
    }

    return client;
});

builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ======================================
// Data Protection (for OAuth cookies)
// ======================================
builder.Services.AddDataProtection()
    .SetApplicationName("IELTSWebApplication");

// ======================================
// Database
// ======================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ======================================
// Dependency Injection
// ======================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddHttpClient<DictionaryApiClient>();
builder.Services.AddScoped<IDictionaryApiClient, DictionaryApiClient>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<IVocabGroupRepository, VocabGroupRepository>();
builder.Services.AddScoped<IVocabGroupService, VocabGroupService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IListeningRepository, ListeningRepository>();
builder.Services.AddScoped<IListeningService, ListeningService>();
builder.Services.AddScoped<IWritingRepository, WritingRepository>();
builder.Services.AddScoped<IWritingService, WritingService>();
builder.Services.AddScoped<ISignInHistoryService, SignInHistoryService>();
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<IWritingFeedbackRepository, WritingFeedbackRepository>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IExamAttemptRepository, ExamAttemptRepository>(); 

builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IWritingFeedbackRepository, WritingFeedbackRepository>();
builder.Services.AddScoped<IWritingFeedbackService, WritingFeedbackService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IVipPlanRepository, VipPlanRepository>();
builder.Services.AddScoped<IVipPlanService, VipPlanService>();

builder.Services.AddScoped<ISpeakingRepository, SpeakingRepository>();
builder.Services.AddScoped<ISpeakingFeedbackRepository, SpeakingFeedbackRepository>();
builder.Services.AddScoped<ISpeakingService, SpeakingService>();
builder.Services.AddScoped<ISpeakingFeedbackService, SpeakingFeedbackService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<ISpeechToTextService, SpeechToTextService>();
builder.Services.AddScoped<IUserSignInHistoryRepository, UserSignInHistoryRepository>();
builder.Services.AddScoped<ISignInHistoryService, SignInHistoryService>();

// Tag Services
builder.Services.AddScoped<ITagService, TagService>();

// Speech to Text
builder.Services.AddScoped<SpeechToTextService>();
//cấu hình Stripe secret key
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
//cấu hình Vip authorize

builder.Services.AddScoped<IVipAuthorizationService, VipAuthorizationService>();
builder.Services.AddScoped<IAuthorizationHandler, VIPAuthorizationHandler>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("VIPOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new VIPRequirement());
    });
});

// ======================================
// CORS (must allow credentials)
// ======================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5173",
            "http://localhost:5173",
            "https://ieltsphobic.web.app/"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ======================================
// Authentication (Google + Cookie)
// ======================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google"; // Google login challenge
})
.AddCookie(options =>
{
    options.Cookie.Name = "IELTSPhobicAuth";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/api/auth/login";
    options.LogoutPath = "/api/auth/logout";

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"error\":\"Unauthorized - Please log in.\"}");
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"error\":\"Access denied - VIP only feature.\"}");
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        }
    };
});
//.AddGoogle("Google", options =>
//{
//    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//    options.CallbackPath = "/api/auth/google/response";
//    options.SaveTokens = true;
//});


// ======================================
// Cookie Policy
// ======================================
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// ======================================
// Middleware order (important!)
// ======================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Handle 401 / 403 as JSON instead of redirect or 404
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        response.ContentType = "application/json";
        await response.WriteAsync("{\"error\":\"Unauthorized - Please log in.\"}");
    }
    else if (response.StatusCode == StatusCodes.Status403Forbidden)
    {
        response.ContentType = "application/json";
        await response.WriteAsync("{\"error\":\"Access denied - VIP only feature.\"}");
    }
});
app.UseCors("AllowReactApp");

// Must be before Authentication
app.UseCookiePolicy();
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
