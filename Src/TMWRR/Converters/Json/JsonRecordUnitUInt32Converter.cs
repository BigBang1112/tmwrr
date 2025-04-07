using ManiaAPI.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TMWRR.Converters.Json;

public class JsonRecordUnitUInt32Converter : JsonConverter<RecordUnit<uint>>
{
    public override RecordUnit<uint> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        var score = reader.GetUInt32();
        reader.Read();
        var count = reader.GetInt32();
        reader.Read();
        return new(score, count);
    }

    public override void Write(Utf8JsonWriter writer, RecordUnit<uint> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Score);
        writer.WriteNumberValue(value.Count);
        writer.WriteEndArray();
    }
}