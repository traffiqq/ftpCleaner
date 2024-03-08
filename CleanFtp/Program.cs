using CleanFtp;
using Microsoft.Extensions.Hosting.Internal;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureHostConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(args);
    })
    .ConfigureAppConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(args);
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
