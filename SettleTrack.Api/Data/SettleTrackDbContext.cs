using Microsoft.EntityFrameworkCore;
using SettleTrack.Api.Models;

namespace SettleTrack.Api.Data;

public class SettleTrackDbContext(DbContextOptions<SettleTrackDbContext> options) : DbContext(options)
{
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.Property(t => t.Isin).HasMaxLength(12).IsRequired();
            entity.Property(t => t.Counterparty).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Currency).HasMaxLength(3).IsRequired();
            entity.Property(t => t.Price).HasPrecision(18, 4);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Side).HasConversion<string>().HasMaxLength(10);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.SettlementDate);
        });
    }
}