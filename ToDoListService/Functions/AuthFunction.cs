using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using ToDoListService.Model;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using System.Text;

public class AuthFunction
{
    private readonly IAuthService _authService;
    public static List<User> _users = new List<User>();

    public AuthFunction(IAuthService authService)
    {
        _authService = authService;
    }

    [Function("Register")]
    [OpenApiOperation(operationId: "Register", tags: new[] { "Auth" })]
    [OpenApiRequestBody("application/json", typeof(User), Required = true, Description = "User credentials")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string), Description = "Success Response")]


    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body, Encoding.UTF8);
        var bodyString = await reader.ReadToEndAsync();
        var requestBody = JsonSerializer.Deserialize<User>(bodyString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Username) || string.IsNullOrWhiteSpace(requestBody.Password))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (_users.Any(u => u.Username == requestBody.Username))
        {
            var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
            await conflictResponse.WriteStringAsync("Username already exists");
            return conflictResponse;
        }

        _users.Add(requestBody); 

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("User registered successfully");
        return response;
    }


    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Auth" })]
    [OpenApiRequestBody("application/json", typeof(User), Required = true, Description = "User credentials")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string), Description = "JWT Token Response")]
    public async Task<HttpResponseData> Login(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body, Encoding.UTF8);
        var bodyString = await reader.ReadToEndAsync();
        var requestBody = JsonSerializer.Deserialize<User>(bodyString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (requestBody == null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var user = _users.FirstOrDefault(u => u.Username == requestBody.Username && u.Password == requestBody.Password);

        if (user == null)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteStringAsync("Invalid username or password");
            return unauthorizedResponse;
        }

        string token = AuthService.GenerateJwtToken(user.Username);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { token });
        return response;
    }
}


