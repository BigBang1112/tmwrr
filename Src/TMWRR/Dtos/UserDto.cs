using TMWRR.Dtos.TMF;

namespace TMWRR.Dtos;

public sealed class UserDto
{
    public Guid Guid { get; set; } = Guid.CreateVersion7();
    public TMFLoginDto? LoginTMF { get; set; }

    //public ICollection<MapDto> Maps { get; set; } = [];
}
