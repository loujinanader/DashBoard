using DashBoard.Broker.Glpi;
using DashBoard.Data;
using DashBoard.Repository;
using DashBoard.Service.DashboardServices;
using DashBoard.Service.GlpiServices;
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

builder.Services.AddDbContext<DashboardDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DashboardDatabase")));
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
