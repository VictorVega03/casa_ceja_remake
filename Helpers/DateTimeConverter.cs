using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CasaCejaRemake.Helpers
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();

            if (string.IsNullOrWhiteSpace(str) || str.StartsWith("0000"))
                return DateTime.UtcNow;

            if (DateTime.TryParse(str, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var result))
            {
                return result;
            }

            // Fallback: intentar sin flags
            if (DateTime.TryParse(str, out var result2))
                return result2;

            Console.WriteLine($"[DateTimeConverter] No se pudo parsear: '{str}' — usando UtcNow");
            return DateTime.UtcNow;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
        }
    }

    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();

            if (string.IsNullOrWhiteSpace(str) || str.StartsWith("0000"))
                return null;

            if (DateTime.TryParse(str, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var result))
            {
                return result;
            }

            if (DateTime.TryParse(str, out var result2))
                return result2;

            Console.WriteLine($"[NullableDateTimeConverter] No se pudo parsear: '{str}' — usando null");
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToUniversalTime().ToString("O"));
            else
                writer.WriteNullValue();
        }
    }
}