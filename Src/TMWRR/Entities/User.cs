using Microsoft.EntityFrameworkCore;

namespace TMWRR.Entities;

[Index(nameof(Guid), IsUnique = true)]
public sealed class User
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.CreateVersion7();
    public TMFLogin? LoginTMF { get; set; }

    public ICollection<Map> Maps { get; set; } = [];
}
