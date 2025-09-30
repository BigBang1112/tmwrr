using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities.TMF;

public class TMFLoginEntity
{
    [StringLength(32)]
    public string Id { get; set; } = string.Empty;

    [StringLength(byte.MaxValue)]
    public string? Nickname { get; set; }

    [StringLength(byte.MaxValue)]
    public string? NicknameDeformatted { get; set; }

    public int? RegistrationId { get; set; }

    public ICollection<TMFCampaignScoresRecordEntity> Records { get; set; } = [];

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Nickname))
        {
            return Id;
        }

        return $"{Id} ({Nickname})";
    }

    public ICollection<UserEntity> Users { get; set; } = [];
}
