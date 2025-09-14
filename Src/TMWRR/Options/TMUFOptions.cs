using System.ComponentModel.DataAnnotations;

namespace TMWRR.Options;

public sealed class TMUFOptions
{
    public bool EnableSoloReport { get; set; }
    public bool EnableLoginDetails { get; set; }
    public bool EnableGhostDownload { get; set; }

    public TimeSpan CheckTimeOfDayCEST { get; set; }
    public TimeSpan CheckRetryTimeout { get; set; }
    public TimeSpan CheckRetryDelay { get; set; }

    public bool Report { get; set; }

    [Required]
    public required TMUFDiscord Discord { get; set; }

    [Required]
    public required TMUFWebServices WebServices { get; set; }
}
