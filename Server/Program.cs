using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: https://incopick.com 에서 오는 요청만 허용, X-Api-Key 헤더 사용 가능
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://incopick.com")
              .AllowAnyMethod()
              .WithHeaders("Content-Type", "X-Api-Key");
    });
});

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    return ConnectionMultiplexer.Connect(config);
});

var app = builder.Build();

app.UseCors();
app.UseMiddleware<PoodleJump.RankingApi.Middleware.ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
