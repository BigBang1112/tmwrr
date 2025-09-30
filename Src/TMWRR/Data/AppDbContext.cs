using Microsoft.EntityFrameworkCore;
using TmEssentials;
using TMWRR.Converters.Db;
using TMWRR.Entities;
using TMWRR.Entities.TMF;

namespace TMWRR.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TMFCampaignEntity> TMFCampaigns { get; set; }
    public DbSet<TMFCampaignScoresSnapshotEntity> TMFCampaignScoresSnapshots { get; set; }
    public DbSet<TMFCampaignScoresRecordEntity> TMFCampaignScoresRecords { get; set; }
    public DbSet<TMFCampaignScoresPlayerCountEntity> TMFCampaignScoresPlayerCounts { get; set; }
    public DbSet<TMFLadderScoresSnapshotEntity> TMFLadderScoresSnapshots { get; set; }
    public DbSet<TMFLadderScoresXYEntity> TMFLadderScoresXYs { get; set; }
    public DbSet<TMFGeneralScoresSnapshotEntity> TMFGeneralScoresSnapshots { get; set; }
    public DbSet<TMFGeneralScoresPlayerEntity> TMFGeneralScoresPlayers { get; set; }
    public DbSet<MapEntity> Maps { get; set; }
    public DbSet<TMFLoginEntity> TMFLogins { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<GameEntity> Games { get; set; }
    public DbSet<ModeEntity> Modes { get; set; }
    public DbSet<TMEnvironmentEntity> Environments { get; set; }
    public DbSet<GhostEntity> Ghosts { get; set; }
    public DbSet<GhostCheckpointEntity> GhostCheckpoints { get; set; }
    public DbSet<ReplayEntity> Replays { get; set; }
    public DbSet<ReplayGhostEntity> ReplayGhosts { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TimeInt32>()
            .HaveConversion<DbTimeInt32Converter>();
    }
}
