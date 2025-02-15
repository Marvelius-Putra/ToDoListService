using System.Security.Claims;

public interface IAuthService
{
    bool RegisterUser(string username, string password);
    bool ValidateUser(string username, string password);
    string GenerateJwtToken(string username);
    ClaimsPrincipal ValidateJwtToken(string token); // Tambahkan ini
}