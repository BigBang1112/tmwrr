using ManiaAPI.Xml.TMUF;
using System.Text.Json.Serialization;
using TMWRR.Converters.Json;
using TMWRR.Models.Resources;

namespace TMWRR;

[JsonSerializable(typeof(GeneralScores))]
[JsonSerializable(typeof(LadderScores))]
[JsonSerializable(typeof(CampaignScores))]
[JsonSerializable(typeof(Dictionary<string, CampaignTMFResource>))]
[JsonSerializable(typeof(Dictionary<string, EnvironmentResource>))]
[JsonSerializable(typeof(Dictionary<string, MapTMFResource>))]
[JsonSerializable(typeof(Dictionary<string, ModeResource>))]
[JsonSerializable(typeof(Dictionary<string, GameResource>))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true,
    Converters = [typeof(JsonTimeInt32Converter), typeof(JsonRecordUnitUInt32Converter), typeof(JsonRecordUnitTimeInt32Converter)])]
internal sealed partial class AppJsonContext : JsonSerializerContext;