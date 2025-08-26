using Microsoft.EntityFrameworkCore;
using TmEssentials;
using TMWRR.Converters.Db;
using TMWRR.Entities;
using TMWRR.Entities.TMF;

namespace TMWRR.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TMFCampaign> TMFCampaigns { get; set; }
    public DbSet<TMFCampaignScoresSnapshot> TMFCampaignScoresSnapshots { get; set; }
    public DbSet<TMFCampaignScoresRecord> TMFCampaignScoresRecords { get; set; }
    public DbSet<TMFCampaignScoresPlayerCount> TMFCampaignScoresPlayerCounts { get; set; }
    public DbSet<TMFLadderScoresSnapshot> TMFLadderScoresSnapshots { get; set; }
    public DbSet<TMFLadderScoresXY> TMFLadderScoresXYs { get; set; }
    public DbSet<Map> Maps { get; set; }
    public DbSet<TMFLogin> TMFLogins { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Mode> Modes { get; set; }
    public DbSet<TMEnvironment> Environments { get; set; }
    public DbSet<Ghost> Ghosts { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TimeInt32>()
            .HaveConversion<DbTimeInt32Converter>();
    }
}
