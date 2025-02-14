using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ToDoListService.Interfaces;
using ToDoListService.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddMemoryCache();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ICheckListService, CheckListService>();
    })
    .Build();

host.Run();
