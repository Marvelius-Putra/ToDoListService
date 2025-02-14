public interface IAuthService
{
    bool RegisterUser(string username, string password);
    bool ValidateUser(string username, string password);
}