using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ToDoListService.Interfaces;
using ToDoListService.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddMemoryCache();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ICheckListService, CheckListService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "http://localhost:7017/",
                    ValidAudience = "http://localhost:7017/",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes("MySuperSecretKeyForJWT123!@#"))))
                };
            });

        services.AddAuthorization();
    })
    .Build();

host.Run();
