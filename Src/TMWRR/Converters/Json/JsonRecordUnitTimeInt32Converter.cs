using ManiaAPI.Xml;
using System.Text.Json;
using System.Text.Json.Serialization;
using TmEssentials;

namespace TMWRR.Converters.Json;

public class JsonRecordUnitTimeInt32Converter : JsonConverter<RecordUnit<TimeInt32>>
{
    public override RecordUnit<TimeInt32> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        var time = new TimeInt32(reader.GetInt32());
        reader.Read();
        var count = reader.GetInt32();
        reader.Read();
        return new(time, count);
    }

    public override void Write(Utf8JsonWriter writer, RecordUnit<TimeInt32> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Score.TotalMilliseconds);
        writer.WriteNumberValue(value.Count);
        writer.WriteEndArray();
    }
}