using DashBoard.Broker.Glpi;
using DashBoard.Data;
using DashBoard.Infrastructure;
using DashBoard.Repository;
using DashBoard.Service.BackgroundServices;
using DashBoard.Service.DashboardServices;
using DashBoard.Service.GlpiServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<IGLPIBroker, GLPIBroker>();
builder.Services.AddScoped<IGLPIService, GLPIService>();
builder.Services.AddScoped<IDashboardServices, DashboardService>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddHostedService<TicketSyncBackgroundService>();

// Force Microsoft.Data.SqlClient's native SNI DLL to load now, while
// running as the real (non-impersonated) identity. The DashBoard bin
// folder lives on H:, a DFS network drive, and lazy-loading that DLL
// later from inside a WindowsImpersonation.RunAsServiceAccount block
// fails with Access Denied, because the impersonated identity's network
// credentials get used for the SMB read of the DLL too. Once loaded, the
// native module stays resident for the rest of the process, so this
// connection attempt failing (wrong server/no access) is fine - only the
// DLL load needs to happen out here.
try
{
    using var warmup = new SqlConnection(builder.Configuration.GetConnectionString("DashboardDatabase"));
    warmup.Open();
}
catch { }

// The DB login (Windows auth) is a service account that may differ from
// whoever is running this process. Kerberos SSO takes over the connection
// with the caller's own domain identity unless we impersonate the service
// account before opening it, so the SqlConnection is created and opened
// here rather than letting EF Core open it from the connection string.
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = new SqlConnection(config.GetConnectionString("DashboardDatabase"));

    var sqlUsername = config["Sql:ImpersonationUsername"];
    if (string.IsNullOrEmpty(sqlUsername))
    {
        connection.Open();
    }
    else
    {
        var sqlDomain = config["Sql:ImpersonationDomain"];
        var sqlPassword = config["Sql:ImpersonationPassword"];
        WindowsImpersonation.RunAsServiceAccount(sqlDomain, sqlUsername, sqlPassword!, connection.Open);
    }

    return connection;
});
builder.Services.AddDbContext<DashboardDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<SqlConnection>()));
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
