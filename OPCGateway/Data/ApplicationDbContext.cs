using Microsoft.EntityFrameworkCore;
using OPCGateway.Data.Entities;

namespace OPCGateway.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ConnectionParameters> ConnectionParameters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ConnectionParameters>().HasKey(cp => cp.Id);
    }
}