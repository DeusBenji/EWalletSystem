using IdentityService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Logging;

public class SafeLogger<T> : ISafeLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly IPiiRedactionService _redactionService;

    public SafeLogger(ILogger<T> logger, IPiiRedactionService redactionService)
    {
        _logger = logger;
        _redactionService = redactionService;
    }

    public void LogDebug(string message, params object[] args)
    {
        ValidateArgs(args);
        _logger.LogDebug(_redactionService.RedactLogMessage(message), args);
    }

    public void LogInformation(string message, params object[] args)
    {
        ValidateArgs(args);
        _logger.LogInformation(_redactionService.RedactLogMessage(message), args);
    }

    public void LogWarning(string message, params object[] args)
    {
        ValidateArgs(args);
        _logger.LogWarning(_redactionService.RedactLogMessage(message), args);
    }

    public void LogError(string message, params object[] args)
    {
        ValidateArgs(args);
        _logger.LogError(_redactionService.RedactLogMessage(message), args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        ValidateArgs(args);
        _logger.LogError(exception, _redactionService.RedactLogMessage(message), args);
    }

    private static void ValidateArgs(object[] args)
    {
        foreach (var arg in args)
        {
            if (arg == null) continue;
            var type = arg.GetType();
            if (!type.IsPrimitive && type != typeof(string) && type != typeof(Guid) && !type.IsEnum && type != typeof(DateTime) && type != typeof(DateTimeOffset))
            {
                throw new InvalidOperationException($"SafeLogger only supports primitive types, strings, Guids, Enums, and Dates. Type {type.Name} is not allowed to prevent PII leakage.");
            }
        }
    }
}
