using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TmEssentials;

namespace TMWRR.Converters.Db;

public class DbTimeInt32Converter : ValueConverter<TimeInt32, int>
{
    public DbTimeInt32Converter()
        : base(
            v => v.TotalMilliseconds,
            v => new TimeInt32(v))
    {
    }
}