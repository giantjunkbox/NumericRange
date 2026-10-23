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

using System.Globalization;
using System.Text.Json.Serialization;

namespace Bitvantage;

/// <summary>
/// An inclusive range of <see cref="ulong"/> values, such as 40-70, or a single value such as 90.
/// </summary>
/// <remarks>
/// A range always holds at least one value, because <see cref="Start"/> can never be greater than <see cref="End"/>.
/// The default value of this struct is the range that holds only the value 0.
/// </remarks>
/// <example>
/// <code>
/// var range = new ValueRange(8000, 8100);
/// var (start, end) = range;    // start = 8000, end = 8100
/// range.Contains(8080);        // true
/// range.Count;                 // 101
/// </code>
/// </example>
public readonly record struct ValueRange
{
    /// <summary>
    /// Creates a range that holds every value from <paramref name="start"/> to <paramref name="end"/>, including both.
    /// </summary>
    /// <param name="start">The first value in the range.</param>
    /// <param name="end">The last value in the range.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="end"/> is less than <paramref name="start"/>.</exception>
    [JsonConstructor]
    public ValueRange(ulong start, ulong end)
    {
        if (start > end)
            throw new ArgumentOutOfRangeException(nameof(end), end, $"The end value must be greater than or equal to the start value {start}");

        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a range that holds the single value <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The only value in the range.</param>
    public ValueRange(ulong value) : this(value, value)
    {
    }

    /// <summary>
    /// The first value in the range.
    /// </summary>
    public ulong Start { get; }

    /// <summary>
    /// The last value in the range.
    /// </summary>
    public ulong End { get; }

    /// <summary>
    /// The number of values in the range.
    /// </summary>
    /// <remarks>
    /// This is a <see cref="UInt128"/> because the range 0-18446744073709551615 holds 2^64 values,
    /// one more than a <see cref="ulong"/> can represent.
    /// It is left out of JSON because it follows from <see cref="Start"/> and <see cref="End"/>.
    /// </remarks>
    [JsonIgnore]
    public UInt128 Count => (UInt128)End - Start + 1;

    /// <summary>
    /// Returns whether the range holds <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to look for.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is between <see cref="Start"/> and <see cref="End"/>, inclusive.</returns>
    public bool Contains(ulong value)
    {
        return Start <= value && value <= End;
    }

    /// <summary>
    /// Returns whether every value in <paramref name="range"/> is also in this range.
    /// </summary>
    /// <param name="range">The range to look for.</param>
    /// <returns><see langword="true"/> if this range covers all of <paramref name="range"/>.</returns>
    public bool Contains(ValueRange range)
    {
        return Start <= range.Start && range.End <= End;
    }

    /// <summary>
    /// Returns whether this range and <paramref name="range"/> have at least one value in common.
    /// </summary>
    /// <param name="range">The range to compare with.</param>
    /// <returns><see langword="true"/> if the two ranges share at least one value.</returns>
    public bool Overlaps(ValueRange range)
    {
        return Start <= range.End && range.Start <= End;
    }

    /// <summary>
    /// Splits the range into its start and end values, so it can be used as <c>var (start, end) = range;</c>.
    /// </summary>
    /// <param name="start">Receives <see cref="Start"/>.</param>
    /// <param name="end">Receives <see cref="End"/>.</param>
    public void Deconstruct(out ulong start, out ulong end)
    {
        start = Start;
        end = End;
    }

    /// <summary>
    /// Converts a single value to a range that holds only that value.
    /// </summary>
    /// <remarks>
    /// This lets a collection expression mix values and ranges, as in
    /// <c>NumericRange ports = [22, 80, new ValueRange(8000, 8100)];</c>.
    /// </remarks>
    /// <param name="value">The value.</param>
    /// <returns>A range from <paramref name="value"/> to <paramref name="value"/>.</returns>
    public static implicit operator ValueRange(ulong value)
    {
        return new ValueRange(value);
    }

    /// <summary>
    /// Returns the range as text, such as <c>40-70</c>, or just the value, such as <c>90</c>, when the range holds a single value.
    /// </summary>
    /// <returns>The range in the same format that <see cref="NumericRange.Parse(string)"/> reads.</returns>
    public override string ToString()
    {
        if (Start == End)
            return Start.ToString(CultureInfo.InvariantCulture);

        return string.Create(CultureInfo.InvariantCulture, $"{Start}-{End}");
    }
}
