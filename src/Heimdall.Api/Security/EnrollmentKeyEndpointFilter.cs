using System.Security.Cryptography;
using System.Text;

namespace Heimdall.Api.Security;

/// <summary>Guards the enrollment endpoint with the bootstrap enrollment key (constant-time SHA-256 comparison).</summary>
internal sealed class EnrollmentKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    private const string HeaderName = "X-Heimdall-Enrollment-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["Heimdall:EnrollmentKey"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) || !KeysMatch(provided, expected))
            return TypedResults.Problem(title: "Invalid enrollment key", statusCode: StatusCodes.Status401Unauthorized);

        return await next(context);
    }

    private static bool KeysMatch(string provided, string expected)
    {
        Span<byte> providedHash = stackalloc byte[32];
        Span<byte> expectedHash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(provided), providedHash);
        SHA256.HashData(Encoding.UTF8.GetBytes(expected), expectedHash);
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
