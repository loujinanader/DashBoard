using DashBoard.Models.Database;
using Microsoft.EntityFrameworkCore;
namespace DashBoard.Data
{
    public class DashboardDbContext : DbContext
    {
        public DashboardDbContext(
            DbContextOptions<DashboardDbContext> options)
            : base(options) { }
        public DbSet<TicketEntity> Tickets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TicketEntity>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<TicketEntity>()
                .Property(t => t.Id)
                .ValueGeneratedNever();
        }
    }
}