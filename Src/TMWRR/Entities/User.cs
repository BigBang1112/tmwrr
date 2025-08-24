using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

[Index(nameof(Guid), IsUnique = true)]
public sealed class User
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; } = Guid.CreateVersion7();

    public TMFLogin? LoginTMF { get; set; }

    public ICollection<Map> Maps { get; set; } = [];
}
