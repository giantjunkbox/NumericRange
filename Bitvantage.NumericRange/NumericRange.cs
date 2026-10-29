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

using System.Collections;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Bitvantage;

/// <summary>
/// A set of <see cref="ulong"/> values stored as sorted, non-overlapping ranges, such as <c>40-70,90,95,300-700,5000</c>.
/// </summary>
/// <remarks>
/// <para>
/// Ranges merge as values are added, so the set is always stored in its shortest form.
/// Adding 71 to <c>40-70</c> gives <c>40-71</c>, and adding 72 to <c>40-71,73-80</c> gives <c>40-80</c>.
/// Lookups take O(log n) time, where n is the number of ranges, not the number of values.
/// Additions and removals also take O(log n) time, plus a small cost for each existing range they merge or remove.
/// </para>
/// <para>
/// Enumerating a <see cref="NumericRange"/> yields its ranges, not its individual values.
/// Use <see cref="Values"/> to enumerate values, but check <see cref="ValueCount"/> first,
/// because a single range can hold up to 2^64 values.
/// </para>
/// <para>
/// The <c>+</c>, <c>-</c>, <c>&amp;</c>, <c>|</c>, <c>^</c> and <c>~</c> operators return a new set and leave their operands unchanged.
/// The <c>+=</c>, <c>-=</c>, <c>&amp;=</c>, <c>|=</c> and <c>^=</c> operators change the set in place.
/// Combine two sets with <c>|</c>; <c>+</c> and <c>+=</c> add a single value or a <see cref="ValueRange"/>.
/// </para>
/// <para>
/// This class is not thread-safe. Changing a set while enumerating it throws <see cref="InvalidOperationException"/>.
/// <see cref="GetHashCode"/> depends on the contents, so do not change a set while it is a key in a dictionary or hash set.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var allowed = NumericRange.Parse("22,80,443,8000-8100");
/// allowed.Contains(8080);              // true
/// allowed.Contains(8000, 8100);        // true, the whole range is allowed
///
/// var requested = NumericRange.Parse("80,443,3389");
/// var granted = allowed &amp; requested;    // 80,443
/// var denied = requested - allowed;    // 3389
/// </code>
/// </example>
[JsonConverter(typeof(NumericRangeJsonConverter))]
[TypeConverter(typeof(NumericRangeTypeConverter))]
public partial class NumericRange : IEnumerable<ValueRange>, IEquatable<NumericRange>
{
    // Orders ranges by start value only. Ranges in the set never overlap, so the start value identifies a range,
    // and a single-value probe such as new ValueRange(x) finds ranges by where they start.
    private static readonly IComparer<ValueRange> StartValueComparer = Comparer<ValueRange>.Create((x, y) => x.Start.CompareTo(y.Start));

    // Invariant: the ranges are sorted, never overlap, and never touch.
    // Two ranges such as 10-20 and 21-30 are always merged into 10-30.
    private readonly SortedSet<ValueRange> _ranges;

    // The number of values in the set. Store and Unstore keep it current, so ValueCount is O(1).
    private UInt128 _valueCount;

    /// <summary>
    /// Creates an empty set.
    /// </summary>
    public NumericRange()
    {
        _ranges = new SortedSet<ValueRange>(StartValueComparer);
    }

    /// <summary>
    /// Creates a copy of <paramref name="numericRange"/>.
    /// Changes to the copy do not affect the original, and changes to the original do not affect the copy.
    /// </summary>
    /// <param name="numericRange">The set to copy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public NumericRange(NumericRange numericRange)
    {
        ArgumentNullException.ThrowIfNull(numericRange);

        // copying a SortedSet that uses the same comparer copies its tree in O(n) instead of inserting every range again
        _ranges = new SortedSet<ValueRange>(numericRange._ranges, StartValueComparer);
        _valueCount = numericRange._valueCount;
    }

    /// <summary>
    /// Creates a set that holds <paramref name="values"/>. The values can be in any order and can repeat.
    /// </summary>
    /// <param name="values">The values to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public NumericRange(IEnumerable<ulong> values) : this()
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var value in values)
            Add(value);
    }

    /// <summary>
    /// Creates a set that holds every value in <paramref name="ranges"/>. The ranges can be in any order and can overlap.
    /// </summary>
    /// <param name="ranges">The ranges to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is <see langword="null"/>.</exception>
    public NumericRange(IEnumerable<ValueRange> ranges) : this()
    {
        ArgumentNullException.ThrowIfNull(ranges);

        foreach (var range in ranges)
            Add(range);
    }

    /// <summary>
    /// Whether the set holds no values.
    /// </summary>
    public bool IsEmpty => _ranges.Count == 0;

    /// <summary>
    /// The number of ranges in the set. For <c>40-70,90,300-700</c> this is 3.
    /// </summary>
    public int RangeCount => _ranges.Count;

    /// <summary>
    /// The number of values in the set. For <c>40-70,90,300-700</c> this is 433.
    /// </summary>
    /// <remarks>
    /// This is a <see cref="UInt128"/> because the set 0-18446744073709551615 holds 2^64 values,
    /// one more than a <see cref="ulong"/> can represent. Reading it is O(1).
    /// </remarks>
    public UInt128 ValueCount => _valueCount;

    /// <summary>
    /// The smallest value in the set, or <see langword="null"/> if the set is empty.
    /// </summary>
    public ulong? Min => IsEmpty ? null : _ranges.Min.Start;

    /// <summary>
    /// The largest value in the set, or <see langword="null"/> if the set is empty.
    /// </summary>
    public ulong? Max => IsEmpty ? null : _ranges.Max.End;

    /// <summary>
    /// Adds a single value.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns><see langword="true"/> if the value was added, or <see langword="false"/> if the set already held it.</returns>
    public bool Add(ulong value)
    {
        return Add(value, value);
    }

    /// <summary>
    /// Adds every value in <paramref name="range"/>.
    /// </summary>
    /// <param name="range">The range to add.</param>
    /// <returns><see langword="true"/> if at least one value was added, or <see langword="false"/> if the set already held them all.</returns>
    public bool Add(ValueRange range)
    {
        return Add(range.Start, range.End);
    }

    /// <summary>
    /// Adds every value from <paramref name="startValue"/> to <paramref name="endValue"/>, including both.
    /// </summary>
    /// <param name="startValue">The first value to add.</param>
    /// <param name="endValue">The last value to add.</param>
    /// <returns><see langword="true"/> if at least one value was added, or <see langword="false"/> if the set already held them all.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endValue"/> is less than <paramref name="startValue"/>.</exception>
    public bool Add(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        var mergedStartValue = startValue;
        var mergedEndValue = endValue;
        var mergedRanges = new List<ValueRange>();

        // look at the range that starts at or before the new range
        if (TryGetRangeStartingAtOrBefore(startValue, out var previousRange))
        {
            // the set already holds every value, so nothing changes
            // Range: 10-30   Add: 15-20   Result: 10-30
            if (previousRange.End >= endValue)
                return false;

            // a range that starts before the new range and overlaps or touches it is merged into it
            // Range: 10-20   Add: 21-25   Result: 10-25
            // a range that starts exactly at startValue is merged by the loop below instead
            if (previousRange.Start < startValue && previousRange.End >= startValue - 1)
            {
                mergedStartValue = previousRange.Start;
                mergedRanges.Add(previousRange);
            }
        }

        // every range that starts inside the new range, or immediately after it, is merged into it
        // Range: 10-20,25,27-30,33-53   Add: 25-40   Result: 10-20,25-53
        // Range: 21-30                  Add: 10-20   Result: 10-30
        foreach (var nextRange in _ranges.GetViewBetween(new ValueRange(startValue), new ValueRange(ulong.MaxValue)))
        {
            // the range neither overlaps nor touches the merged range, and no later range can either
            // nothing can start after ulong.MaxValue, so the + 1 is skipped there to avoid wrapping around to 0
            if (mergedEndValue != ulong.MaxValue && nextRange.Start > mergedEndValue + 1)
                break;

            mergedEndValue = Math.Max(mergedEndValue, nextRange.End);
            mergedRanges.Add(nextRange);
        }

        foreach (var range in mergedRanges)
            Unstore(range);

        Store(new ValueRange(mergedStartValue, mergedEndValue));

        // the new range was not inside a single existing range, and the gaps between ranges mean
        // it could not be covered by several of them either, so at least one value was added
        return true;
    }

    /// <summary>
    /// Removes a single value.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true"/> if the value was removed, or <see langword="false"/> if the set did not hold it.</returns>
    public bool Remove(ulong value)
    {
        return Remove(value, value);
    }

    /// <summary>
    /// Removes every value in <paramref name="range"/>.
    /// </summary>
    /// <param name="range">The range to remove.</param>
    /// <returns><see langword="true"/> if at least one value was removed, or <see langword="false"/> if the set held none of them.</returns>
    public bool Remove(ValueRange range)
    {
        return Remove(range.Start, range.End);
    }

    /// <summary>
    /// Removes every value from <paramref name="startValue"/> to <paramref name="endValue"/>, including both.
    /// </summary>
    /// <param name="startValue">The first value to remove.</param>
    /// <param name="endValue">The last value to remove.</param>
    /// <returns><see langword="true"/> if at least one value was removed, or <see langword="false"/> if the set held none of them.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endValue"/> is less than <paramref name="startValue"/>.</exception>
    public bool Remove(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        // collect the overlapping ranges first, because the loop below changes the set
        var overlappingRanges = GetOverlappingRanges(startValue, endValue).ToList();

        // remove each overlapping range and put back whatever sticks out on either side of the removed values
        // Range: 100-150   Remove: 90-120    Result: 121-150
        // Range: 100-150   Remove: 120-200   Result: 100-119
        // Range: 100-150   Remove: 125       Result: 100-124,126-150
        // Range: 100-150   Remove: 0-999     Result: (empty)
        // the comparisons guarantee startValue > 0 before - 1 and endValue < ulong.MaxValue before + 1
        foreach (var range in overlappingRanges)
        {
            Unstore(range);

            if (range.Start < startValue)
                Store(new ValueRange(range.Start, startValue - 1));

            if (range.End > endValue)
                Store(new ValueRange(endValue + 1, range.End));
        }

        return overlappingRanges.Count > 0;
    }

    /// <summary>
    /// Removes every value.
    /// </summary>
    public void Clear()
    {
        _ranges.Clear();
        _valueCount = 0;
    }

    /// <summary>
    /// Returns whether the set holds <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to look for.</param>
    /// <returns><see langword="true"/> if the set holds <paramref name="value"/>.</returns>
    public bool Contains(ulong value)
    {
        return TryGetRange(value, out _);
    }

    /// <summary>
    /// Returns whether the set holds every value from <paramref name="startValue"/> to <paramref name="endValue"/>, including both.
    /// </summary>
    /// <param name="startValue">The first value to look for.</param>
    /// <param name="endValue">The last value to look for.</param>
    /// <returns><see langword="true"/> if the set holds all of the values.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endValue"/> is less than <paramref name="startValue"/>.</exception>
    public bool Contains(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        // ranges never touch, so a run of consecutive values can only be covered by a single range
        return TryGetRangeStartingAtOrBefore(startValue, out var range) && range.End >= endValue;
    }

    /// <summary>
    /// Returns whether the set holds every value in <paramref name="range"/>.
    /// </summary>
    /// <param name="range">The range to look for.</param>
    /// <returns><see langword="true"/> if the set holds all of the values in <paramref name="range"/>.</returns>
    public bool Contains(ValueRange range)
    {
        return Contains(range.Start, range.End);
    }

    /// <summary>
    /// Returns whether the set holds at least one value from <paramref name="startValue"/> to <paramref name="endValue"/>, including both.
    /// </summary>
    /// <param name="startValue">The first value to look for.</param>
    /// <param name="endValue">The last value to look for.</param>
    /// <returns><see langword="true"/> if the set holds any of the values.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endValue"/> is less than <paramref name="startValue"/>.</exception>
    public bool Overlaps(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        // ranges are sorted and never overlap, so the last range starting at or before endValue also ends last;
        // if it ends before startValue, every earlier range does too
        return TryGetRangeStartingAtOrBefore(endValue, out var range) && range.End >= startValue;
    }

    /// <summary>
    /// Returns whether the set holds at least one value in <paramref name="range"/>.
    /// </summary>
    /// <param name="range">The range to look for.</param>
    /// <returns><see langword="true"/> if the set holds any of the values in <paramref name="range"/>.</returns>
    public bool Overlaps(ValueRange range)
    {
        return Overlaps(range.Start, range.End);
    }

    /// <summary>
    /// Finds the range in the set that holds <paramref name="value"/>.
    /// For example, in <c>22,80,8000-8100</c> the value 8080 is held by the range <c>8000-8100</c>.
    /// </summary>
    /// <param name="value">The value to look for.</param>
    /// <param name="range">Receives the range that holds <paramref name="value"/>, or <see langword="default"/> if there is none.</param>
    /// <returns><see langword="true"/> if the set holds <paramref name="value"/>.</returns>
    public bool TryGetRange(ulong value, out ValueRange range)
    {
        if (TryGetRangeStartingAtOrBefore(value, out range) && range.End >= value)
            return true;

        range = default;
        return false;
    }

    /// <summary>
    /// Returns the values from <paramref name="startValue"/> to <paramref name="endValue"/> that are not in the set.
    /// </summary>
    /// <param name="startValue">The first value to consider.</param>
    /// <param name="endValue">The last value to consider.</param>
    /// <returns>A new set with the missing values. This set is not changed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endValue"/> is less than <paramref name="startValue"/>.</exception>
    /// <example>
    /// Find the first free port at or above 1024:
    /// <code>
    /// var used = NumericRange.Parse("1024-1030,1032");
    /// var firstFree = used.Complement(1024, 65535).Min;    // 1031
    /// </code>
    /// </example>
    public NumericRange Complement(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        var result = new NumericRange();

        // walk the ranges that overlap the requested span in order, collecting the gaps between them
        // nextValue is the first value that has not been accounted for yet
        var nextValue = startValue;
        foreach (var range in GetOverlappingRanges(startValue, endValue))
        {
            if (range.Start > nextValue)
                result.Store(new ValueRange(nextValue, range.Start - 1));

            // this range reaches the end of the span, so there is no gap after it
            if (range.End >= endValue)
                return result;

            nextValue = range.End + 1;
        }

        // the gaps are separated by ranges of this set, so they never touch and can be stored directly
        result.Store(new ValueRange(nextValue, endValue));
        return result;
    }

    /// <summary>
    /// Returns every value from 0 to <see cref="ulong.MaxValue"/> that is not in the set.
    /// </summary>
    /// <returns>A new set with the missing values. This set is not changed.</returns>
    public NumericRange Complement()
    {
        return Complement(ulong.MinValue, ulong.MaxValue);
    }

    /// <summary>
    /// Returns a copy of the set. Changes to the copy do not affect the original, and changes to the original do not affect the copy.
    /// </summary>
    /// <returns>A new set with the same values.</returns>
    public NumericRange Clone()
    {
        return new NumericRange(this);
    }

    /// <summary>
    /// Returns the ranges in the set in ascending order.
    /// </summary>
    /// <returns>The ranges, such as <c>40-70</c>, <c>90</c> and <c>300-700</c> for the set <c>40-70,90,300-700</c>.</returns>
    public IEnumerable<ValueRange> Ranges()
    {
        // iterate rather than return the SortedSet itself, so callers cannot cast it back and change it
        foreach (var range in _ranges)
            yield return range;
    }

    /// <summary>
    /// Returns every individual value in the set in ascending order.
    /// </summary>
    /// <remarks>
    /// The values are produced one at a time, but a single range can hold up to 2^64 values.
    /// Check <see cref="ValueCount"/> before calling methods such as <c>ToList</c> or <c>Count</c> on the result.
    /// </remarks>
    /// <returns>The values, such as 40, 41, 42 and so on for the set <c>40-70</c>.</returns>
    public IEnumerable<ulong> Values()
    {
        foreach (var range in _ranges)
        {
            // stop at the end value explicitly, because incrementing past ulong.MaxValue wraps around to 0
            var value = range.Start;
            while (true)
            {
                yield return value;

                if (value == range.End)
                    break;

                value++;
            }
        }
    }

    /// <summary>
    /// Returns an enumerator over the ranges in the set in ascending order.
    /// </summary>
    /// <returns>An enumerator over the ranges, not the individual values.</returns>
    public IEnumerator<ValueRange> GetEnumerator()
    {
        return _ranges.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns whether <paramref name="other"/> holds exactly the same values as this set.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if both sets hold the same values.</returns>
    public bool Equals(NumericRange? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        // both sets are stored in their shortest form, so equal sets have identical ranges;
        // comparing the counts first rules out most unequal sets without walking the ranges
        return _ranges.Count == other._ranges.Count
               && _valueCount == other._valueCount
               && _ranges.SequenceEqual(other._ranges);
    }

    /// <summary>
    /// Returns whether <paramref name="obj"/> is a <see cref="NumericRange"/> that holds exactly the same values as this set.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a set with the same values.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as NumericRange);
    }

    /// <summary>
    /// Returns a hash code based on the values in the set.
    /// </summary>
    /// <remarks>
    /// Computing the hash code takes O(n) time. The hash code changes when the set changes,
    /// so do not change a set while it is a key in a dictionary or hash set.
    /// </remarks>
    /// <returns>A hash code that is equal for sets with the same values.</returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var range in _ranges)
            hashCode.Add(range);

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns the set as text, such as <c>40-70,90,300-700</c>, or an empty string if the set is empty.
    /// </summary>
    /// <returns>The set in the same format that <see cref="Parse(string)"/> reads.</returns>
    public override string ToString()
    {
        return string.Join(',', _ranges);
    }

    // Finds the range with the largest start value that is less than or equal to value.
    // That range is the only one that can hold value, and the only earlier range that can overlap or touch
    // anything starting at value. Takes O(log n) time.
    private bool TryGetRangeStartingAtOrBefore(ulong value, out ValueRange range)
    {
        range = _ranges.GetViewBetween(new ValueRange(ulong.MinValue), new ValueRange(value)).Max;

        // an empty view returns default(ValueRange), which is also the real range 0-0;
        // if a range starting at 0 is stored, it is inside the view, so the view was not empty
        return range != default || _ranges.Contains(default);
    }

    // Returns, in order, every range that holds at least one value from startValue to endValue.
    // The caller must not change the set while enumerating the result.
    private IEnumerable<ValueRange> GetOverlappingRanges(ulong startValue, ulong endValue)
    {
        // a range that starts before startValue can still reach into the requested values
        // Range: 110-120   Values: 115-200   the range 110-120 starts before 115 but overlaps it
        if (startValue > 0 && TryGetRangeStartingAtOrBefore(startValue - 1, out var previousRange) && previousRange.End >= startValue)
            yield return previousRange;

        // every range that starts inside the requested values overlaps them
        foreach (var range in _ranges.GetViewBetween(new ValueRange(startValue), new ValueRange(endValue)))
            yield return range;
    }

    // Adds a range that does not overlap or touch any stored range, and updates the value count.
    private void Store(ValueRange range)
    {
        _ranges.Add(range);
        _valueCount += range.Count;
    }

    // Removes a stored range and updates the value count.
    private void Unstore(ValueRange range)
    {
        _ranges.Remove(range);
        _valueCount -= range.Count;
    }

    private static void ValidateRange(ulong startValue, ulong endValue)
    {
        if (startValue > endValue)
            throw new ArgumentOutOfRangeException(nameof(endValue), endValue, $"The end value must be greater than or equal to the start value {startValue}");
    }
}
