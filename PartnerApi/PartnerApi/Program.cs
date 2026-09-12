using Microsoft.OpenApi;
using PartnerApi.Logging;
using PartnerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure log4net Provider Options
builder.Logging.ClearProviders();
builder.Logging.AddLog4Net(new Log4NetProviderOptions
{
    Log4NetConfigFileName = "log4net.config",
    Watch = true
});

// 2. Register Dependency Injection Services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPartnerRegistry, PartnerRegistry>();
builder.Services.AddSingleton<IPaymentSignatureGenerator, PaymentSignatureGenerator>();
builder.Services.AddSingleton<IPaymentProcessingService, PaymentProcessingService>();
builder.Services.AddControllers();

// 3. Register Swagger OpenAPI UI Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Partner Transaction Web API",
        Version = "v1",
        Description = "REST API Middleware for partner authentication, signature validation, discount processing, and log encryption."
    });
});

var app = builder.Build();

// 4. Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Partner API v1");
    c.RoutePrefix = "swagger"; 
});

// 5. Enable Logging Middleware
app.UseMiddleware<Log4NetLoggingMiddleware>();
app.MapControllers();

app.Run();