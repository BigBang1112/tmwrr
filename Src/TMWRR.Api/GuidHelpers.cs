namespace TMWRR.Api;

public static class GuidHelpers
{
    public static string ToBase64String(Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public static Guid FromBase64String(string base64)
    {
        var paddedBase64 = base64
            .Replace("-", "+")
            .Replace("_", "/");

        switch (paddedBase64.Length % 4)
        {
            case 2: paddedBase64 += "=="; break;
            case 3: paddedBase64 += "="; break;
        }

        return new Guid(Convert.FromBase64String(paddedBase64));
    }

    public static bool TryParseOrEncoded(string guidOrEncoded, out Guid guid)
    {
        // First try to parse as a standard GUID
        if (Guid.TryParse(guidOrEncoded, out guid))
        {
            return true;
        }

        if (guidOrEncoded.Length != 22)
        {
            return false;
        }

        try
        {
            guid = FromBase64String(guidOrEncoded);
        }
        catch
        {
            return false;
        }

        return true;
    }
}
