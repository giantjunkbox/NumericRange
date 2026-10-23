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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bitvantage;

// Parsing of text such as "40-70,90,95,300-700,5000".
//
// Format:
//   list  = empty | item ("," item)*
//   item  = value | value "-" value
//   value = one or more ASCII digits 0-9, at most ulong.MaxValue
//
// Whitespace is allowed around values, commas and dashes. Empty or whitespace-only text is the empty set,
// which matches what ToString returns for an empty set. Items can be in any order and can overlap.
// Empty items such as "1,,2" or a trailing comma such as "1,2," are rejected.
public partial class NumericRange : ISpanParsable<NumericRange>
{
    /// <summary>
    /// Parses text such as <c>40-70,90,95,300-700,5000</c> into a set.
    /// </summary>
    /// <remarks>
    /// Items are separated by commas, and each item is a single value or two values joined by a dash.
    /// Whitespace around values, commas and dashes is ignored. Items can be in any order and can overlap.
    /// Empty or whitespace-only text gives an empty set.
    /// </remarks>
    /// <param name="value">The text to parse.</param>
    /// <returns>A new set with the parsed values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is not in the expected format, holds a value larger than <see cref="ulong.MaxValue"/>,
    /// or holds a range whose end is less than its start. The message gives the position of the problem.
    /// </exception>
    public static NumericRange Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Parse(value.AsSpan());
    }

    /// <summary>
    /// Parses text such as <c>40-70,90,95,300-700,5000</c> into a set.
    /// </summary>
    /// <remarks>
    /// Accepts the same format as <see cref="Parse(string)"/>.
    /// </remarks>
    /// <param name="value">The text to parse.</param>
    /// <returns>A new set with the parsed values.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is not in the expected format, holds a value larger than <see cref="ulong.MaxValue"/>,
    /// or holds a range whose end is less than its start. The message gives the position of the problem.
    /// </exception>
    public static NumericRange Parse(ReadOnlySpan<char> value)
    {
        var result = new NumericRange();
        if (!TryParseCore(value, result, out var error))
            throw new FormatException(error);

        return result;
    }

    /// <summary>
    /// Tries to parse text such as <c>40-70,90,95,300-700,5000</c> into a set, without throwing on invalid text.
    /// </summary>
    /// <remarks>
    /// Accepts the same format as <see cref="Parse(string)"/>.
    /// </remarks>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">Receives the parsed set, or <see langword="null"/> if the text is invalid.</param>
    /// <returns><see langword="true"/> if the text was parsed.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out NumericRange? result)
    {
        if (value is null)
        {
            result = null;
            return false;
        }

        return TryParse(value.AsSpan(), out result);
    }

    /// <summary>
    /// Tries to parse text such as <c>40-70,90,95,300-700,5000</c> into a set, without throwing on invalid text.
    /// </summary>
    /// <remarks>
    /// Accepts the same format as <see cref="Parse(string)"/>.
    /// </remarks>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">Receives the parsed set, or <see langword="null"/> if the text is invalid.</param>
    /// <returns><see langword="true"/> if the text was parsed.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out NumericRange? result)
    {
        var numericRange = new NumericRange();
        if (TryParseCore(value, numericRange, out _))
        {
            result = numericRange;
            return true;
        }

        result = null;
        return false;
    }

    // The IParsable and ISpanParsable members let generic code, ASP.NET Core minimal APIs and other frameworks parse a NumericRange.
    // They are explicit because the format provider is not used: the format is always ASCII digits, commas and dashes.

    static NumericRange IParsable<NumericRange>.Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    static bool IParsable<NumericRange>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NumericRange result)
    {
        return TryParse(s, out result);
    }

    static NumericRange ISpanParsable<NumericRange>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    static bool ISpanParsable<NumericRange>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out NumericRange result)
    {
        return TryParse(s, out result);
    }

    // Parses text into result one item at a time. On failure, error describes the problem and its position.
    private static bool TryParseCore(ReadOnlySpan<char> text, NumericRange result, [NotNullWhen(false)] out string? error)
    {
        var position = SkipWhitespace(text, 0);

        // empty or whitespace-only text is the empty set
        if (position == text.Length)
        {
            error = null;
            return true;
        }

        while (true)
        {
            var itemPosition = position;
            if (!TryParseValue(text, ref position, out var startValue, out error))
                return false;

            var endValue = startValue;
            position = SkipWhitespace(text, position);

            // an optional "-end" turns the single value into a range
            if (position < text.Length && text[position] == '-')
            {
                position = SkipWhitespace(text, position + 1);
                if (!TryParseValue(text, ref position, out endValue, out error))
                    return false;

                if (startValue > endValue)
                {
                    error = string.Create(CultureInfo.InvariantCulture, $"The range {startValue}-{endValue} at position {itemPosition} ends before it starts");
                    return false;
                }

                position = SkipWhitespace(text, position);
            }

            result.Add(startValue, endValue);

            if (position == text.Length)
            {
                error = null;
                return true;
            }

            // anything after an item other than a comma is an error, for example "1 2" or "1-2-3"
            if (text[position] != ',')
            {
                error = $"Unexpected character '{text[position]}' at position {position}; expected ',' or '-'";
                return false;
            }

            position = SkipWhitespace(text, position + 1);
        }
    }

    // Reads a run of ASCII digits starting at position and advances position past it.
    // char.IsAsciiDigit is used instead of char.IsDigit, which also accepts digits from other scripts that ulong.Parse rejects.
    private static bool TryParseValue(ReadOnlySpan<char> text, ref int position, out ulong value, [NotNullWhen(false)] out string? error)
    {
        var start = position;
        while (position < text.Length && char.IsAsciiDigit(text[position]))
            position++;

        if (position == start)
        {
            value = 0;
            error = position == text.Length
                ? $"Expected a number at position {position}, but the text ended"
                : $"Expected a number at position {position}, but found '{text[position]}'";
            return false;
        }

        // the span holds only ASCII digits, so the only way this can fail is a value larger than ulong.MaxValue
        if (!ulong.TryParse(text[start..position], NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            error = $"The value {text[start..position]} at position {start} is larger than {ulong.MaxValue}";
            return false;
        }

        error = null;
        return true;
    }

    private static int SkipWhitespace(ReadOnlySpan<char> text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
            position++;

        return position;
    }
}
