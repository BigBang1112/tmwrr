using Microsoft.EntityFrameworkCore;

namespace TMWRR.Entities.TMF;

[Index(nameof(Guid), IsUnique = true)]
[Index(nameof(CreatedAt), IsUnique = true)]
public class TMFLadderScoresSnapshotEntity
{
    public int Id { get; set; }

    public Guid Guid { get; set; } = Guid.CreateVersion7();

    public int PlayerCount { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset PublishedAt { get; set; }
    //public string? Etag { get; set; } not returned from ManiaAPI

    public bool NoChanges { get; set; }

    public ICollection<TMFLadderScoresXYEntity> XYs { get; set; } = [];

    public override string ToString()
    {
        return $"{CreatedAt:yyyy-MM-dd HH:mm:ss} ({XYs.Count} graph points)";
    }
}