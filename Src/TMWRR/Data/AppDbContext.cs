using Microsoft.EntityFrameworkCore;
using TMWRR.Entities;

namespace TMWRR.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TMUFCampaign> TMUFCampaigns { get; set; }
    public DbSet<TMUFScoresSnapshot> TMUFScoresSnapshots { get; set; }
}
