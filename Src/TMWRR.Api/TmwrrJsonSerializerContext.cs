using System.Text.Json.Serialization;
using TMWRR.Api.Converters.Json;
using TMWRR.Api.TMF;

namespace TMWRR.Api;

[JsonSerializable(typeof(TmwrrInformation))]
[JsonSerializable(typeof(IEnumerable<Map>))]
[JsonSerializable(typeof(TMFCampaignScoresSnapshot))]
[JsonSourceGenerationOptions(Converters = [typeof(JsonStringEnumConverter), typeof(JsonTimeInt32Converter)], PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class TmwrrJsonSerializerContext : JsonSerializerContext;