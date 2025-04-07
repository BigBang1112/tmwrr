using ManiaAPI.Xml.TMUF;
using System.Text.Json.Serialization;
using TMWRR.Converters.Json;

namespace TMWRR;

[JsonSerializable(typeof(GeneralScores))]
[JsonSerializable(typeof(LadderScores))]
[JsonSerializable(typeof(CampaignScores))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true,
    Converters = [typeof(JsonTimeInt32Converter), typeof(JsonRecordUnitUInt32Converter), typeof(JsonRecordUnitTimeInt32Converter)])]
internal sealed partial class AppJsonContext : JsonSerializerContext;