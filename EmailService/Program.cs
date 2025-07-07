using EmailService;
using EmailService.Services;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddHostedService<Worker>();
    });

await builder.RunConsoleAsync();
