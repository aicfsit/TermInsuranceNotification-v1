using TermInsuranceNotification.Abstractions;
using static TermInsuranceNotification.Model.DBModel;

namespace TermInsuranceNotification.Services
{
    /// <summary>
    /// Loads the HTML body from EmailTemplate\{TemplateFile}.
    /// Falls back to the Email_Body column when no file is configured or found.
    /// Templates are cached - they only change on deployment.
    /// </summary>
    public class FileTemplateProvider : ITemplateProvider
    {
        private readonly string _templateFolder;
        private readonly ILogger<FileTemplateProvider> _logger;
        private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        public FileTemplateProvider(ILogger<FileTemplateProvider> logger)
        {
            _logger = logger;
            _templateFolder = Path.Combine(AppContext.BaseDirectory, "EmailTemplate");
        }

        public string GetTemplate(NotificationConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.TemplateFile))
                return config.EmailBody;

            if (_cache.TryGetValue(config.TemplateFile, out var cached))
                return cached;

            var path = Path.Combine(_templateFolder, config.TemplateFile);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Template '{file}' not found at {path}; using the DB body instead.",
                    config.TemplateFile, path);
                return config.EmailBody;
            }

            var html = File.ReadAllText(path);
            _cache[config.TemplateFile] = html;
            return html;
        }
    }
}
