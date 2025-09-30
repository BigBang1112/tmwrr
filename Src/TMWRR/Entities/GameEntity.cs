using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public sealed class GameEntity
{
    [StringLength(12)]
    public string Id { get; set; } = string.Empty;

    public ICollection<TMEnvironmentEntity> Environments { get; set; } = [];

    public override string ToString()
    {
        return Id;
    }
}
