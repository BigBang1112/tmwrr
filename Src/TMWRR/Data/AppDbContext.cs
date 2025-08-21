using Microsoft.EntityFrameworkCore;
using TMWRR.Entities;

namespace TMWRR.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TMFCampaign> TMFCampaigns { get; set; }
    public DbSet<TMFCampaignScoresSnapshot> TMFCampaignScoresSnapshots { get; set; }
    public DbSet<TMFCampaignScoresRecord> TMFCampaignScoresRecords { get; set; }
    public DbSet<Map> Maps { get; set; }
    public DbSet<TMFLogin> TMFLogins { get; set; }
}
