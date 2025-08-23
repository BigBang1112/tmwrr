using System.ComponentModel.DataAnnotations;

namespace TMWRR.Entities;

public class TMFLogin
{
    [StringLength(32)]
    public string Id { get; set; } = string.Empty;

    [StringLength(byte.MaxValue)]
    public string? Nickname { get; set; }

    public ICollection<TMFCampaignScoresRecord> Records { get; set; } = [];

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Nickname))
        {
            return Id;
        }

        return $"{Id} ({Nickname})";
    }

    public ICollection<User> Users { get; set; } = [];
}
