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
using System.Text;
using System.Text.RegularExpressions;

namespace Bitvantage;

public class NumericRange
{
    private static readonly Regex RangeRegex = new(@"^((((?<start>[0-9]+)((-(?<end>[0-9]+))|(?<end>)))(,|$))*)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    private readonly SortedSet<ValueRange> _values = new();

    public void Add(ulong value)
    {
        Add(value, value);
    }

    // TODO: overwrite add, and subtract?
    public void Add(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        var removeList = new List<ValueRange>();
        var mergedStartValue = startValue;
        var mergedEndValue = endValue;

        // merge the range that starts before the new range if it overlaps or touches it
        // for example adding 21-23 to 10-20,25 should become 10-23,25
        if (startValue > 0)
        {
            var previousRange = _values.GetViewBetween(ValueRange.Min, new ValueRange(startValue - 1)).Max;
            if (previousRange.IsSet && previousRange.EndValue >= startValue - 1)
            {
                mergedStartValue = previousRange.StartValue;
                mergedEndValue = Math.Max(mergedEndValue, previousRange.EndValue);
                removeList.Add(previousRange);
            }
        }

        // merge every range that starts inside the new range or immediately after it
        // for example adding 25-40 to 10-20,25,27-30,33-53 should become 10-20,25-53
        foreach (var nextRange in _values.GetViewBetween(new ValueRange(startValue), ValueRange.Max))
        {
            if (mergedEndValue != ulong.MaxValue && nextRange.StartValue > mergedEndValue + 1)
                break;

            mergedEndValue = Math.Max(mergedEndValue, nextRange.EndValue);
            removeList.Add(nextRange);
        }

        foreach (var valueRange in removeList)
            _values.Remove(valueRange);

        _values.Add(new ValueRange(mergedStartValue, mergedEndValue));
    }

    public void Add(NumericRange numericRange)
    {
        ArgumentNullException.ThrowIfNull(numericRange);

        // adding a range to itself changes nothing, and enumerating our own set while modifying it would throw
        if (ReferenceEquals(numericRange, this))
            return;

        foreach (var range in numericRange._values)
            Add(range.StartValue, range.EndValue);
    }

    public void Remove(NumericRange numericRange)
    {
        ArgumentNullException.ThrowIfNull(numericRange);

        // removing a range from itself empties it, and enumerating our own set while modifying it would throw
        if (ReferenceEquals(numericRange, this))
        {
            Clear();
            return;
        }

        foreach (var range in numericRange._values)
            Remove(range.StartValue, range.EndValue);
    }


    public void Clear()
    {
        _values.Clear();
    }

    public bool Contains(ulong value)
    {
        var previousValues = _values.GetViewBetween(new ValueRange(0), value > 0 ? new ValueRange(value) : ValueRange.Min);

        var previousValueMax = previousValues.Max;

        if (!previousValueMax.IsSet)
            return false;

        return previousValueMax.StartValue <= value && value <= previousValueMax.EndValue;
    }

    public static NumericRange operator +(NumericRange numericRange, ulong value)
    {
        numericRange.Add(value);
        return numericRange;
    }

    public static NumericRange Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var match = RangeRegex.Match(value);

        if (!match.Success)
            throw new ArgumentException("Could not parse string");

        var range = new NumericRange();

        for (var i = 0; i < match.Groups["start"].Captures.Count; i++)
        {
            var startValueString = match.Groups["start"].Captures[i].Value;
            var endValueString = match.Groups["end"].Captures[i].Value;

            if (!ulong.TryParse(startValueString, NumberStyles.None, CultureInfo.InvariantCulture, out var startValue))
                throw new ArgumentException($"The value {startValueString} is too large", nameof(value));

            var endValue = startValue;
            if (endValueString != string.Empty && !ulong.TryParse(endValueString, NumberStyles.None, CultureInfo.InvariantCulture, out endValue))
                throw new ArgumentException($"The value {endValueString} is too large", nameof(value));

            if (startValue > endValue)
                throw new ArgumentException($"The range {startValueString}-{endValueString} ends before it starts", nameof(value));

            range.Add(startValue, endValue);
        }

        return range;
    }

    public void Remove(ulong value)
    {
        Remove(value, value);
    }

    public void Remove(ulong startValue, ulong endValue)
    {
        ValidateRange(startValue, endValue);

        var overlappingRanges = new List<ValueRange>();

        // the range that starts before the removal range can still overlap it
        // Range:  10-100, 110-120
        // Remove: 115-200
        // the range 110-120 starts before 115 but overlaps it
        if (startValue > 0)
        {
            var previousRange = _values.GetViewBetween(ValueRange.Min, new ValueRange(startValue - 1)).Max;
            if (previousRange.IsSet && previousRange.EndValue >= startValue)
                overlappingRanges.Add(previousRange);
        }

        // every range that starts inside the removal range overlaps it
        overlappingRanges.AddRange(_values.GetViewBetween(new ValueRange(startValue), new ValueRange(endValue)));

        // remove each overlapping range and add back whatever sticks out on either side of the removal range
        // Range:  100-150   Remove: 90-120    Result: 121-150
        // Range:  100-150   Remove: 120-200   Result: 100-119
        // Range:  100-150   Remove: 125       Result: 100-124,126-150
        // Range:  100-150   Remove: 0-999     Result: (empty)
        foreach (var range in overlappingRanges)
        {
            _values.Remove(range);

            if (range.StartValue < startValue)
                _values.Add(new ValueRange(range.StartValue, startValue - 1));

            if (range.EndValue > endValue)
                _values.Add(new ValueRange(endValue + 1, range.EndValue));
        }
    }

    private static void ValidateRange(ulong startValue, ulong endValue)
    {
        if (startValue > endValue)
            throw new ArgumentOutOfRangeException(nameof(endValue), endValue, $"The end value must be greater than or equal to the start value {startValue}");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var value in _values)
        {
            if (sb.Length > 0)
                sb.Append(',');

            sb.Append(value);
        }

        return sb.ToString();
    }

    //public static NumericRange operator -(NumericRange numericRange, ulong value)
    //{
    //    numericRange.Remove(value);
    //    return numericRange;
    //}

    public IEnumerable<ulong> Values()
    {
        foreach (var value in _values)
        {
            // stop at the end value explicitly, because incrementing past ulong.MaxValue wraps around to 0
            var i = value.StartValue;
            while (true)
            {
                yield return i;

                if (i == value.EndValue)
                    break;

                i++;
            }
        }
    }

    public IEnumerable<string> Ranges()
    {
        return _values.Select(value => value.ToString());
    }

    private readonly struct ValueRange : IComparable<ValueRange>
    {
        public readonly ulong StartValue;
        public readonly ulong EndValue;

        public static ValueRange Max => new(ulong.MaxValue);
        public static ValueRange Min => new(ulong.MinValue);

        public readonly bool IsSet;

        public ValueRange(ulong startValue, ulong endValue)
        {
            StartValue = startValue;
            EndValue = endValue;
            IsSet = true;
        }


        public ValueRange(ulong startValue)
        {
            StartValue = startValue;
            EndValue = startValue;
        }

        public override string ToString()
        {
            if (StartValue == EndValue)
                return StartValue.ToString();

            return $"{StartValue}-{EndValue}";
        }

        public int CompareTo(ValueRange other)
        {
            return StartValue.CompareTo(other.StartValue);
            //var startValueComparison = StartValue.CompareTo(other.StartValue);
            //if (startValueComparison != 0) 
            //    return startValueComparison;

            //return EndValue.CompareTo(other.EndValue);
        }
    }
}
