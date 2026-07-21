using TermInsuranceNotification;
using TermInsuranceNotification.Helper;
using TermInsuranceNotification.Repository;

var builder = Host.CreateApplicationBuilder(args);

// Bind the "ApplicationLogs" section (email API settings, testing flags, connection strings)
var appConfig = builder.Configuration.GetSection("ApplicationLogs").Get<ApplicationLogsConfig>()
                ?? new ApplicationLogsConfig();
builder.Services.AddSingleton(appConfig);

// Repository points at the ERP DB where tbl_client_notification_details and the views live.
var erpConnection = builder.Configuration["ApplicationLogs:ERP_IBS"] ?? string.Empty;
builder.Services.AddSingleton(new NotificationRepository(erpConnection));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
