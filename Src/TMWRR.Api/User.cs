using TMWRR.Api.TMF;

namespace TMWRR.Api;

public sealed class User
{
    public Guid Guid { get; set; } = Guid.CreateVersion7();
    public TMFLogin? LoginTMF { get; set; }

    //public ICollection<MapDto> Maps { get; set; } = [];
}
