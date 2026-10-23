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

// Binary operators (+, -, &, ^) return a new set and never change their operands.
// Compound assignment operators (+=, -=, &=, ^=) use the C# 14 instance operator syntax and change the set in place,
// so x += 5 costs the same as x.Add(5). Code compiled with C# 13 or earlier, or written in another .NET language,
// treats x += 5 as x = x + 5 instead, which gives the same result by copying the set.
public partial class NumericRange
{
    /// <summary>
    /// Returns a new set that holds every value in <paramref name="numericRange"/> plus <paramref name="value"/>.
    /// </summary>
    /// <param name="numericRange">The set to copy. It is not changed.</param>
    /// <param name="value">The value to add to the copy.</param>
    /// <returns>A new set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public static NumericRange operator +(NumericRange numericRange, ulong value)
    {
        var result = new NumericRange(numericRange);
        result.Add(value);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds every value in <paramref name="numericRange"/> plus every value in <paramref name="range"/>.
    /// </summary>
    /// <param name="numericRange">The set to copy. It is not changed.</param>
    /// <param name="range">The range to add to the copy.</param>
    /// <returns>A new set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public static NumericRange operator +(NumericRange numericRange, ValueRange range)
    {
        var result = new NumericRange(numericRange);
        result.Add(range);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds every value in either <paramref name="left"/> or <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The first set. It is not changed.</param>
    /// <param name="right">The second set. It is not changed.</param>
    /// <returns>A new set with the union of both sets.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static NumericRange operator +(NumericRange left, NumericRange right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        // copy the set with more ranges and add the smaller one to it, which needs fewer insertions
        var (smaller, larger) = OrderByRangeCount(left, right);
        var result = new NumericRange(larger);
        result.UnionWith(smaller);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds every value in <paramref name="numericRange"/> except <paramref name="value"/>.
    /// </summary>
    /// <param name="numericRange">The set to copy. It is not changed.</param>
    /// <param name="value">The value to remove from the copy.</param>
    /// <returns>A new set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public static NumericRange operator -(NumericRange numericRange, ulong value)
    {
        var result = new NumericRange(numericRange);
        result.Remove(value);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds every value in <paramref name="numericRange"/> except those in <paramref name="range"/>.
    /// </summary>
    /// <param name="numericRange">The set to copy. It is not changed.</param>
    /// <param name="range">The range to remove from the copy.</param>
    /// <returns>A new set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public static NumericRange operator -(NumericRange numericRange, ValueRange range)
    {
        var result = new NumericRange(numericRange);
        result.Remove(range);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds every value in <paramref name="left"/> that is not in <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The set to copy. It is not changed.</param>
    /// <param name="right">The values to leave out. It is not changed.</param>
    /// <returns>A new set with the difference of the two sets.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static NumericRange operator -(NumericRange left, NumericRange right)
    {
        ArgumentNullException.ThrowIfNull(right);

        var result = new NumericRange(left);
        result.ExceptWith(right);
        return result;
    }

    /// <summary>
    /// Returns a new set that holds only the values that are in both <paramref name="left"/> and <paramref name="right"/>.
    /// </summary>
    /// <param name="left">The first set. It is not changed.</param>
    /// <param name="right">The second set. It is not changed.</param>
    /// <returns>A new set with the intersection of both sets.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static NumericRange operator &(NumericRange left, NumericRange right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        // build the result from the shared ranges directly instead of copying either set
        var result = new NumericRange();
        foreach (var range in IntersectRanges(left, right))
            result.Store(range);

        return result;
    }

    /// <summary>
    /// Returns a new set that holds the values that are in exactly one of <paramref name="left"/> and <paramref name="right"/>, but not in both.
    /// </summary>
    /// <param name="left">The first set. It is not changed.</param>
    /// <param name="right">The second set. It is not changed.</param>
    /// <returns>A new set with the symmetric difference of both sets.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
    public static NumericRange operator ^(NumericRange left, NumericRange right)
    {
        ArgumentNullException.ThrowIfNull(right);

        var result = new NumericRange(left);
        result.SymmetricExceptWith(right);
        return result;
    }

    /// <summary>
    /// Adds <paramref name="value"/> to this set in place. Same as <see cref="Add(ulong)"/>.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public void operator +=(ulong value)
    {
        Add(value);
    }

    /// <summary>
    /// Adds every value in <paramref name="range"/> to this set in place. Same as <see cref="Add(ValueRange)"/>.
    /// </summary>
    /// <param name="range">The range to add.</param>
    public void operator +=(ValueRange range)
    {
        Add(range);
    }

    /// <summary>
    /// Adds every value in <paramref name="numericRange"/> to this set in place. Same as <see cref="UnionWith"/>.
    /// </summary>
    /// <param name="numericRange">The set whose values to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public void operator +=(NumericRange numericRange)
    {
        UnionWith(numericRange);
    }

    /// <summary>
    /// Removes <paramref name="value"/> from this set in place. Same as <see cref="Remove(ulong)"/>.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    public void operator -=(ulong value)
    {
        Remove(value);
    }

    /// <summary>
    /// Removes every value in <paramref name="range"/> from this set in place. Same as <see cref="Remove(ValueRange)"/>.
    /// </summary>
    /// <param name="range">The range to remove.</param>
    public void operator -=(ValueRange range)
    {
        Remove(range);
    }

    /// <summary>
    /// Removes every value in <paramref name="numericRange"/> from this set in place. Same as <see cref="ExceptWith"/>.
    /// </summary>
    /// <param name="numericRange">The set whose values to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public void operator -=(NumericRange numericRange)
    {
        ExceptWith(numericRange);
    }

    /// <summary>
    /// Keeps only the values that are also in <paramref name="numericRange"/>, in place. Same as <see cref="IntersectWith"/>.
    /// </summary>
    /// <param name="numericRange">The set to intersect with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public void operator &=(NumericRange numericRange)
    {
        IntersectWith(numericRange);
    }

    /// <summary>
    /// Keeps the values that are in exactly one of the two sets, in place. Same as <see cref="SymmetricExceptWith"/>.
    /// </summary>
    /// <param name="numericRange">The set to compare with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="numericRange"/> is <see langword="null"/>.</exception>
    public void operator ^=(NumericRange numericRange)
    {
        SymmetricExceptWith(numericRange);
    }

    /// <summary>
    /// Returns whether two sets hold exactly the same values. Two <see langword="null"/> references are equal.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true"/> if both sets hold the same values, or both are <see langword="null"/>.</returns>
    public static bool operator ==(NumericRange? left, NumericRange? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>
    /// Returns whether two sets hold different values.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true"/> if the sets hold different values, or only one is <see langword="null"/>.</returns>
    public static bool operator !=(NumericRange? left, NumericRange? right)
    {
        return !(left == right);
    }
}
