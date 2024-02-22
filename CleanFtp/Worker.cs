using FluentFTP;

namespace CleanFtp;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private string? Host => Environment.GetEnvironmentVariable("FTP_HOST");
    private string? User => Environment.GetEnvironmentVariable("FTP_USER");
    private string? Password => Environment.GetEnvironmentVariable("FTP_PASSWORD");
    private string? Directory => Environment.GetEnvironmentVariable("FTP_DIRECTORY");

    private int? DeleteOlderThanXDays
    {
        get
        {
            var deleteOlderThanXDays = Environment.GetEnvironmentVariable("DeleteOlderThanXDays");
            if (string.IsNullOrEmpty(deleteOlderThanXDays)) return null;

            if (!int.TryParse(deleteOlderThanXDays, out var days))
            {
                _logger.LogError("Could not parse DeleteOlderThanXDays: '{day}'", deleteOlderThanXDays);
                return null;
            }

            if (days >= 0) return days;

            _logger.LogError("DeleteOlderThanXDays must be positive: '{day}'", deleteOlderThanXDays);
            return null;
        }
    }
    
    private int CycleTimeInHours
    {
        get
        {
            var deleteOlderThanXDays = Environment.GetEnvironmentVariable("CycleTimeInHours");
            if (string.IsNullOrEmpty(deleteOlderThanXDays))
            {
                _logger.LogError("CycleTimeInHours not set, defaulting to 24 hours.");
                return 24;
            }

            if (!int.TryParse(deleteOlderThanXDays, out var hours))
            {
                _logger.LogError("Could not parse CycleTimeInHours: '{day}', defaulting to 24 hours.", deleteOlderThanXDays);
                return 24;
            }

            if (hours >= 0) return hours;

            _logger.LogError("CycleTimeInHours must be positive: '{day}', defaulting to 24 hours.", deleteOlderThanXDays);
            return 24;
        }
    }    

    private bool DryRun
    {
        get
        {
            var dryRun = Environment.GetEnvironmentVariable("DRY_RUN");
            if (string.IsNullOrEmpty(dryRun))
            {
                return true;
            }

            return !dryRun.Equals("N", StringComparison.InvariantCulture);
        }
    }

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker running at: {time}", DateTimeOffset.Now);

            var client = GetFtpClient();
            if (client == null)
            {
                _logger.LogError("Connection failed.");
                return;
            }

            if (!client.DirectoryExists(this.Directory))
            {
                _logger.LogError("Directory '{dir}' does not exist.", this.Directory);
                return;
            }

            var data = client.GetListing(this.Directory, FtpListOption.Recursive).ToArray();

            var deletionRangeInDays = this.DeleteOlderThanXDays;
            if (deletionRangeInDays == null)
            {
                _logger.LogError("DeleteOlderThanXDays not specified");
                return;
            }

            foreach (var file in data.Where(_ => _.Type == FtpObjectType.File))
            {
                if (file.Modified < DateTime.Now.AddDays(-deletionRangeInDays.Value))
                {
                    if (this.DryRun)
                    {
                        _logger.LogInformation("DryRun: delete {file} : {days} days old - {timestamp}", file.FullName, (DateTime.Now - file.Modified).Days, file.Modified);
                        continue;
                    }
                    
                    _logger.LogInformation("deleted {file} : {days} days old - {timestamp}", file.FullName, (DateTime.Now - file.Modified).Days, file.Modified);
                    client.DeleteFile(file.FullName);
                }
            }
            
            // clear empty folders
            foreach (var directory in data.Where(_ => _.Type == FtpObjectType.Directory))
            {
                var files = client.GetListing(directory.FullName, FtpListOption.Recursive).ToArray();
                if (files.Any(_ => _.Type == FtpObjectType.File))
                {
                    continue;
                }
                
                if (this.DryRun)
                {
                    _logger.LogInformation("DryRun: delete empty folder {folder}", directory.FullName);
                    continue;
                }
                
                _logger.LogInformation("deleted empty folder {folder}", directory.FullName);
                client.DeleteDirectory(directory.FullName);
            }

            await Task.Delay(TimeSpan.FromHours(this.CycleTimeInHours), stoppingToken);
        }
    }


    private FtpClient? GetFtpClient()
    {
        if (string.IsNullOrEmpty(this.Host))
        {
            _logger.LogError("Host not specified");
            return null;
        }

        if (string.IsNullOrEmpty(this.User))
        {
            _logger.LogError("User not specified");
            return null;
        }

        if (string.IsNullOrEmpty(this.Password))
        {
            _logger.LogError("Password not specified");
            return null;
        }

        if (this.DeleteOlderThanXDays == null)
        {
            _logger.LogError("DeleteOlderThanXDays not specified");
            return null;
        }

        var client = new FtpClient(this.Host, this.User, this.Password);

        try
        {
            client.AutoConnect();
        }
        catch (Exception e)
        {
            _logger.LogError("{Message}", e.Message);
            return null;
        }

        return client;
    }
}