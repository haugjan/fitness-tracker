using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FitnessTracker.Auth;

public sealed record PinAuthOptions(string Pin, string JwtKey, int TokenLifetimeDays);

/// <summary>
/// One person, one PIN. The PIN is traded once for a long lived bearer token, so the phone
/// asks for it after an install and then stays signed in.
/// </summary>
public static class PinAuthentication
{
    public const string Issuer = "fitness-tracker";

    public static IServiceCollection AddPinAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var pin = configuration["Auth:Pin"];
        if (string.IsNullOrWhiteSpace(pin))
        {
            throw new InvalidOperationException("Auth:Pin must be configured.");
        }

        var jwtKey = configuration["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Auth:JwtKey must be configured.");
            }

            // A throwaway key keeps local runs working, it only invalidates tokens on restart.
            jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("Auth:JwtKey must be at least 32 bytes long.");
        }

        var lifetimeDays = configuration.GetValue("Auth:TokenLifetimeDays", 180);
        services.AddSingleton(new PinAuthOptions(pin, jwtKey, lifetimeDays));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });

        // Kein Fallback-Policy: statische Dateien laufen in .NET 10 ebenfalls über
        // Endpoints und würden sonst 401 liefern. Geschützt wird die API-Gruppe.
        services.AddAuthorization();

        return services;
    }

    /// <summary>Compares hashes, so the duration of the check says nothing about the PIN.</summary>
    public static bool PinMatches(string? provided, string expected)
    {
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? string.Empty));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }

    public static LoginResponse IssueToken(PinAuthOptions options, DateTimeOffset now)
    {
        var expiresUtc = now.AddDays(options.TokenLifetimeDays);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtKey));

        var token = new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Issuer,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = expiresUtc.UtcDateTime,
                Claims = new Dictionary<string, object> { ["sub"] = "owner" },
                SigningCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256
                ),
            }
        );

        return new LoginResponse(token, expiresUtc);
    }
}
