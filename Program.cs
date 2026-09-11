using TermInsuranceNotification;
using TermInsuranceNotification.Abstractions;
using TermInsuranceNotification.Helper;
using TermInsuranceNotification.Repository;
using TermInsuranceNotification.Services;

var builder = Host.CreateApplicationBuilder(args);

// "ApplicationLogs" section: connection strings, email API settings, testing switch.
var appConfig = builder.Configuration.GetSection("ApplicationLogs").Get<ApplicationLogsConfig>()
                ?? new ApplicationLogsConfig();
builder.Services.AddSingleton(appConfig);

// ---- one registration per responsibility; everything is resolved by interface ----

var erpConnection = builder.Configuration["ApplicationLogs:ERP_IBS"] ?? string.Empty;
builder.Services.AddSingleton<INotificationRepository>(new NotificationRepository(erpConnection));

builder.Services.AddSingleton<ITemplateProvider, FileTemplateProvider>();
builder.Services.AddSingleton<ITemplateRenderer, PlaceholderTemplateRenderer>();
builder.Services.AddSingleton<IRecipientResolver, RecipientResolver>();
builder.Services.AddSingleton<IEmailSender, EmailApiSender>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
