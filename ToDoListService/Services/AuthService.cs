using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private static readonly string SecretKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("MySuperSecretKeyForJWT123!@#"));
    private const int TokenExpirationMinutes = 60;

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, string> _users = new(); // Simulasi database user

    public AuthService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool RegisterUser(string username, string password)
    {
        if (_users.ContainsKey(username)) return false; // Cek apakah user sudah terdaftar

        string hashedPassword = HashPassword(password);
        _users[username] = hashedPassword;
        return true;
    }

    public bool ValidateUser(string username, string password)
    {
        return _users.TryGetValue(username, out string storedPassword) && VerifyPassword(password, storedPassword);
    }

    public string GenerateJwtToken(string username)
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }


    public ClaimsPrincipal ValidateJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(SecretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        string hash = HashPassword(password);
        return hash == storedHash;
    }
}
