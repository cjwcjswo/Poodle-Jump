namespace PoodleJump.RankingApi.Middleware;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderNameConfig = "ApiKey:HeaderName";
    private const string ApiKeyValidKeyConfig = "ApiKey:ValidKey";
    private const string ApiSettingsValidKeyConfig = "ApiSettings:ValidKey";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

	// ApiKeyMiddleware.cs ?????? InvokeAsync ????? ?????? ???? ?????????
	public async Task InvokeAsync(HttpContext context)
	{
		// 1. Swagger ???? ????? ?????? ????????.
		if (context.Request.Path.StartsWithSegments("/swagger") ||
			context.Request.Path.StartsWithSegments("/swagger-ui"))
		{
			await _next(context);
			return;
		}

		var headerName = _configuration[ApiKeyHeaderNameConfig] ?? "X-Api-Key";
		var validKey = _configuration[ApiKeyValidKeyConfig]
			?? _configuration[ApiSettingsValidKeyConfig];

		if (string.IsNullOrWhiteSpace(validKey))
		{
			await _next(context);
			return;
		}

		if (!context.Request.Headers.TryGetValue(headerName, out var providedKey) || providedKey != validKey)
		{
			_logger.LogWarning("API Key authentication failed from {RemoteIp}", context.Connection.RemoteIpAddress);
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await context.Response.WriteAsync("Unauthorized");
			return;
		}

		await _next(context);
	}
}
