/*
   Bitvantage.NumericRange
   Copyright (C) 2024 Michael Crino
   
   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.
   
   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.
   
   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bitvantage;

/// <summary>
/// Reads and writes a <see cref="NumericRange"/> as a JSON string such as <c>"80,443,8000-8100"</c>.
/// </summary>
/// <remarks>
/// <see cref="NumericRange"/> uses this converter automatically, so it does not need to be registered.
/// Without it, System.Text.Json would treat the set as a list of ranges, and a setting such as
/// <c>"AllowedPorts": "80,443"</c> in a configuration file could not be read.
/// </remarks>
public sealed class NumericRangeJsonConverter : JsonConverter<NumericRange>
{
    /// <summary>
    /// Reads a JSON string such as <c>"80,443,8000-8100"</c> as a <see cref="NumericRange"/>.
    /// </summary>
    /// <param name="reader">The reader positioned on the value.</param>
    /// <param name="typeToConvert">The type being read, which is <see cref="NumericRange"/>.</param>
    /// <param name="options">The serializer options in use.</param>
    /// <returns>The parsed set.</returns>
    /// <exception cref="JsonException">The value is not a string, or the string is not a valid set.</exception>
    public override NumericRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a string such as \"80,443,8000-8100\" for a {nameof(NumericRange)}, but found {reader.TokenType}");

        var text = reader.GetString()!;

        try
        {
            return NumericRange.Parse(text);
        }
        catch (FormatException exception)
        {
            // JsonException makes the serializer add the JSON path of the bad value to the message
            throw new JsonException($"'{text}' is not a valid {nameof(NumericRange)}: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Writes a <see cref="NumericRange"/> as a JSON string such as <c>"80,443,8000-8100"</c>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The set to write.</param>
    /// <param name="options">The serializer options in use.</param>
    public override void Write(Utf8JsonWriter writer, NumericRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
