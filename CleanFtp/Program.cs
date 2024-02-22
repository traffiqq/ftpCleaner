using CleanFtp;

IHost host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    .ConfigureHostConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(args);
    })
    .Build();

host.Run();