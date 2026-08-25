using DashBoard.Service.GlpiServices;
namespace DashBoard.Service.BackgroundServices
{
    public class TicketSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TicketSyncBackgroundService> _logger;
        public TicketSyncBackgroundService(IServiceScopeFactory scopeFactory,ILogger<TicketSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the application a few seconds to start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var glpiService =scope.ServiceProvider.GetRequiredService<IGLPIService>();
                    _logger.LogInformation("Starting GLPI ticket synchronization...");
                    await glpiService.SyncTicketsAsync();
                    _logger.LogInformation("GLPI ticket synchronization completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Error while synchronizing GLPI tickets.");
                }

                // Sync every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5),stoppingToken);
            }
        }
    }
}