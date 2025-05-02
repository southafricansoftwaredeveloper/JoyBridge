using Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace EventBridge;

public class EventDb : DbContext
{
    public DbSet<PatientEvent> PatientEvents => Set<PatientEvent>();

    // we should update this to make it configurable in the future
    protected override void OnConfiguring(DbContextOptionsBuilder b)
    {
        b.UseNpgsql("Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=dev");
    }

}