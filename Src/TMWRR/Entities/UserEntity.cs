using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TMWRR.Entities.TMF;

namespace TMWRR.Entities;

[Index(nameof(Guid), IsUnique = true)]
public sealed class UserEntity
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; } = Guid.CreateVersion7();

    public TMFLoginEntity? LoginTMF { get; set; }

    public ICollection<MapEntity> Maps { get; set; } = [];
}
