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

namespace Bitvantage;

// Operations that combine or compare two sets. The method names match ISet<T>, so they read the same as HashSet<T> and SortedSet<T>.
public partial class NumericRange
{
    /// <summary>
    /// Adds every value in <paramref name="numericRange"/> to this set.
    /// </summary>
    /// <param name="numericRange">The set whose values to add.</param>
    /// <returns><see langword="true"/> if at least one value was added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public bool Add(NumericRange numericRange)
    {
        var valueCount = _valueCount;
        UnionWith(numericRange);
        return _valueCount != valueCount;
    }

    /// <summary>
    /// Removes every value in <paramref name="numericRange"/> from this set.
    /// </summary>
    /// <param name="numericRange">The set whose values to remove.</param>
    /// <returns><see langword="true"/> if at least one value was removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public bool Remove(NumericRange numericRange)
    {
        var valueCount = _valueCount;
        ExceptWith(numericRange);
        return _valueCount != valueCount;
    }

    /// <summary>
    /// Changes this set to hold every value that is in this set, in <paramref name="other"/>, or in both.
    /// </summary>
    /// <param name="other">The set to combine with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void UnionWith(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // a set is already its own union, and enumerating our own ranges while adding to them would throw
        if (ReferenceEquals(other, this))
            return;

        foreach (var range in other._ranges)
            Add(range);
    }

    /// <summary>
    /// Changes this set to hold only the values that are also in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The set to intersect with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void IntersectWith(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        var commonRanges = IntersectRanges(this, other);

        Clear();
        foreach (var range in commonRanges)
            Store(range);
    }

    /// <summary>
    /// Removes every value in <paramref name="other"/> from this set.
    /// </summary>
    /// <param name="other">The set whose values to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void ExceptWith(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        // only the values both sets share need removing; finding them walks the smaller set,
        // which is much faster than removing every range of a large other set
        foreach (var range in IntersectRanges(this, other))
            Remove(range);
    }

    /// <summary>
    /// Changes this set to hold every value that is in exactly one of this set and <paramref name="other"/>, but not in both.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public void SymmetricExceptWith(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        // (this ∪ other) − (this ∩ other); the shared values must be found before the union changes this set
        var commonRanges = IntersectRanges(this, other);

        UnionWith(other);
        foreach (var range in commonRanges)
            Remove(range);
    }

    /// <summary>
    /// Returns whether this set and <paramref name="other"/> have at least one value in common.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if the sets share at least one value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool Overlaps(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return !IsEmpty;

        // look each range of the smaller set up in the larger one, stopping at the first match
        var (smaller, larger) = OrderByRangeCount(this, other);
        foreach (var range in smaller._ranges)
            if (larger.Overlaps(range))
                return true;

        return false;
    }

    /// <summary>
    /// Returns whether every value in this set is also in <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if this set is a subset of <paramref name="other"/>. An empty set is a subset of every set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool IsSubsetOf(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return true;

        // a set with more values cannot fit inside a set with fewer
        if (_valueCount > other._valueCount)
            return false;

        foreach (var range in _ranges)
            if (!other.Contains(range))
                return false;

        return true;
    }

    /// <summary>
    /// Returns whether every value in <paramref name="other"/> is also in this set.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if this set is a superset of <paramref name="other"/>. Every set is a superset of an empty set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool IsSupersetOf(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return other.IsSubsetOf(this);
    }

    /// <summary>
    /// Returns whether every value in this set is also in <paramref name="other"/>, and <paramref name="other"/> holds at least one more value.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if this set is a proper subset of <paramref name="other"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool IsProperSubsetOf(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return _valueCount < other._valueCount && IsSubsetOf(other);
    }

    /// <summary>
    /// Returns whether every value in <paramref name="other"/> is also in this set, and this set holds at least one more value.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if this set is a proper superset of <paramref name="other"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool IsProperSupersetOf(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return other.IsProperSubsetOf(this);
    }

    /// <summary>
    /// Returns whether this set and <paramref name="other"/> hold exactly the same values. This is the same as <see cref="Equals(NumericRange)"/>.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true"/> if both sets hold the same values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool SetEquals(NumericRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Equals(other);
    }

    // Returns, in ascending order, the ranges of values that both sets hold.
    // Walks the set with fewer ranges and looks each of its ranges up in the other, so the cost is
    // O(m log n + k), where m is the smaller range count, n the larger, and k the number of ranges returned.
    private static List<ValueRange> IntersectRanges(NumericRange first, NumericRange second)
    {
        var (smaller, larger) = OrderByRangeCount(first, second);

        // the pieces never overlap or touch: two touching pieces would need two touching ranges in one of the sets,
        // which the stored form rules out, so the result can be stored without merging
        var commonRanges = new List<ValueRange>();
        foreach (var range in smaller._ranges)
            foreach (var overlappingRange in larger.GetOverlappingRanges(range.Start, range.End))
                commonRanges.Add(new ValueRange(Math.Max(range.Start, overlappingRange.Start), Math.Min(range.End, overlappingRange.End)));

        return commonRanges;
    }

    private static (NumericRange Smaller, NumericRange Larger) OrderByRangeCount(NumericRange first, NumericRange second)
    {
        return first._ranges.Count <= second._ranges.Count ? (first, second) : (second, first);
    }
}
