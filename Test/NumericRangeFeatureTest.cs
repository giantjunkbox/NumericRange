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

using System.ComponentModel;
using System.Text.Json;
using Bitvantage;

namespace Test
{
    // Builds the expected text for a plain set of values, merging consecutive values into ranges.
    internal static class RangeModel
    {
        public static string ToRangeString(IEnumerable<ulong> values)
        {
            var ranges = new List<string>();
            ulong? startValue = null;
            ulong previousValue = 0;

            foreach (var value in values.Order())
            {
                if (startValue != null && value == previousValue + 1)
                {
                    previousValue = value;
                    continue;
                }

                if (startValue != null)
                    ranges.Add(startValue == previousValue ? $"{startValue}" : $"{startValue}-{previousValue}");

                startValue = value;
                previousValue = value;
            }

            if (startValue != null)
                ranges.Add(startValue == previousValue ? $"{startValue}" : $"{startValue}-{previousValue}");

            return string.Join(",", ranges);
        }
    }

    internal class ValueRangeTest
    {
        [Test]
        public void ConstructorRejectsReversedRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ValueRange(20, 10));
        }

        [Test]
        public void Properties()
        {
            var range = new ValueRange(8000, 8100);
            var (start, end) = range;

            Assert.That(range.Start, Is.EqualTo(8000UL));
            Assert.That(range.End, Is.EqualTo(8100UL));
            Assert.That(start, Is.EqualTo(8000UL));
            Assert.That(end, Is.EqualTo(8100UL));
            Assert.That(range.Count, Is.EqualTo((UInt128)101));
            Assert.That(range.ToString(), Is.EqualTo("8000-8100"));
            Assert.That(new ValueRange(90).ToString(), Is.EqualTo("90"));
            Assert.That(default(ValueRange).ToString(), Is.EqualTo("0"));
            Assert.That(new ValueRange(0, ulong.MaxValue).Count, Is.EqualTo((UInt128)ulong.MaxValue + 1));
        }

        [Test]
        public void ContainsAndOverlaps()
        {
            var range = new ValueRange(10, 20);

            Assert.That(range.Contains(10), Is.True);
            Assert.That(range.Contains(20), Is.True);
            Assert.That(range.Contains(21), Is.False);
            Assert.That(range.Contains(new ValueRange(12, 20)), Is.True);
            Assert.That(range.Contains(new ValueRange(12, 21)), Is.False);
            Assert.That(range.Overlaps(new ValueRange(20, 30)), Is.True);
            Assert.That(range.Overlaps(new ValueRange(0, 10)), Is.True);
            Assert.That(range.Overlaps(new ValueRange(21, 30)), Is.False);
        }

        [Test]
        public void Equality()
        {
            var range = new ValueRange(1, 5);
            var same = new ValueRange(1, 5);
            ValueRange single = 7;

            Assert.That(range, Is.EqualTo(same));
            Assert.That(range == same, Is.True);
            Assert.That(range, Is.Not.EqualTo(new ValueRange(1, 6)));
            Assert.That(single, Is.EqualTo(new ValueRange(7, 7)));
        }
    }

    internal class NumericRangeFeatureTest
    {
        [Test]
        public void CountsAndBounds()
        {
            var range = NumericRange.Parse("40-70,90,300-700");
            Assert.That(range.IsEmpty, Is.False);
            Assert.That(range.RangeCount, Is.EqualTo(3));
            Assert.That(range.ValueCount, Is.EqualTo((UInt128)433));
            Assert.That(range.Min, Is.EqualTo(40UL));
            Assert.That(range.Max, Is.EqualTo(700UL));

            var empty = new NumericRange();
            Assert.That(empty.IsEmpty, Is.True);
            Assert.That(empty.RangeCount, Is.EqualTo(0));
            Assert.That(empty.ValueCount, Is.EqualTo(UInt128.Zero));
            Assert.That(empty.Min, Is.Null);
            Assert.That(empty.Max, Is.Null);

            var full = NumericRange.Parse("0-18446744073709551615");
            Assert.That(full.ValueCount, Is.EqualTo((UInt128)ulong.MaxValue + 1));
            Assert.That(full.Min, Is.EqualTo(0UL));
            Assert.That(full.Max, Is.EqualTo(ulong.MaxValue));

            full.Remove(5);
            Assert.That(full.ValueCount, Is.EqualTo((UInt128)ulong.MaxValue));

            full.Clear();
            Assert.That(full.ValueCount, Is.EqualTo(UInt128.Zero));
        }

        [Test]
        public void AddAndRemoveReportWhetherAnythingChanged()
        {
            var range = NumericRange.Parse("10-30");

            Assert.That(range.Add(15, 20), Is.False);
            Assert.That(range.Add(10), Is.False);
            Assert.That(range.Add(new ValueRange(30)), Is.False);
            Assert.That(range.Add(31), Is.True);
            Assert.That(range.ToString(), Is.EqualTo("10-31"));
            Assert.That(range.Add(25, 40), Is.True);
            Assert.That(range.Remove(50, 60), Is.False);
            Assert.That(range.Remove(new ValueRange(5, 10)), Is.True);
            Assert.That(range.ToString(), Is.EqualTo("11-40"));

            Assert.That(range.Add(range.Clone()), Is.False);
            Assert.That(range.Add(NumericRange.Parse("11,41")), Is.True);
            Assert.That(range.Remove(NumericRange.Parse("100")), Is.False);
            Assert.That(range.Remove(NumericRange.Parse("11,100")), Is.True);
            Assert.That(range.ToString(), Is.EqualTo("12-41"));
        }

        [Test]
        public void RangeQueries()
        {
            var range = NumericRange.Parse("22,80,8000-8100");

            Assert.That(range.Contains(8000, 8100), Is.True);
            Assert.That(range.Contains(8050, 8060), Is.True);
            Assert.That(range.Contains(8000, 8101), Is.False);
            Assert.That(range.Contains(21, 22), Is.False);
            Assert.That(range.Contains(new ValueRange(22)), Is.True);

            Assert.That(range.Overlaps(8100, 9000), Is.True);
            Assert.That(range.Overlaps(0, 22), Is.True);
            Assert.That(range.Overlaps(23, 79), Is.False);
            Assert.That(range.Overlaps(8101, ulong.MaxValue), Is.False);
            Assert.That(range.Overlaps(new ValueRange(80)), Is.True);
            Assert.That(new NumericRange().Overlaps(0, ulong.MaxValue), Is.False);

            Assert.That(range.TryGetRange(8080, out var found), Is.True);
            Assert.That(found, Is.EqualTo(new ValueRange(8000, 8100)));
            Assert.That(range.TryGetRange(81, out found), Is.False);
            Assert.That(found, Is.EqualTo(default(ValueRange)));

            Assert.Throws<ArgumentOutOfRangeException>(() => range.Contains(20, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Overlaps(20, 10));
        }

        [Test]
        public void RangeStartingAtZero()
        {
            // the range 0-0 is also default(ValueRange), which the lookups must tell apart from "not found"
            var range = NumericRange.Parse("0,5");

            Assert.That(range.Contains(0), Is.True);
            Assert.That(range.Contains(1), Is.False);
            Assert.That(range.TryGetRange(0, out var found), Is.True);
            Assert.That(found, Is.EqualTo(new ValueRange(0)));
            Assert.That(range.Overlaps(0, 0), Is.True);
            Assert.That(range.Add(1), Is.True);
            Assert.That(range.ToString(), Is.EqualTo("0-1,5"));
            Assert.That(new NumericRange().Contains(0), Is.False);
        }

        [Test]
        public void Complement()
        {
            var used = NumericRange.Parse("1024-1030,1032");

            Assert.That(used.Complement(1024, 65535).ToString(), Is.EqualTo("1031,1033-65535"));
            Assert.That(used.Complement(1024, 65535).Min, Is.EqualTo(1031UL));
            Assert.That(used.Complement(1025, 1030).IsEmpty, Is.True);
            Assert.That(used.Complement(0, 10).ToString(), Is.EqualTo("0-10"));
            Assert.That(used.Complement(1030, 1033).ToString(), Is.EqualTo("1031,1033"));
            Assert.That(used.Complement().ToString(), Is.EqualTo("0-1023,1031,1033-18446744073709551615"));
            Assert.That(new NumericRange().Complement().ToString(), Is.EqualTo("0-18446744073709551615"));
            Assert.That(NumericRange.Parse("0-18446744073709551615").Complement().IsEmpty, Is.True);
            Assert.That(used.ToString(), Is.EqualTo("1024-1030,1032"));
        }

        [Test]
        public void SetOperators()
        {
            var allowed = NumericRange.Parse("22,80,443,8000-8100");
            var requested = NumericRange.Parse("80,443,3389,8050-8200");

            Assert.That((allowed & requested).ToString(), Is.EqualTo("80,443,8050-8100"));
            Assert.That((requested - allowed).ToString(), Is.EqualTo("3389,8101-8200"));
            Assert.That((allowed + requested).ToString(), Is.EqualTo("22,80,443,3389,8000-8200"));
            Assert.That((allowed ^ requested).ToString(), Is.EqualTo("22,3389,8000-8049,8101-8200"));
            Assert.That((allowed + new ValueRange(1, 21)).ToString(), Is.EqualTo("1-22,80,443,8000-8100"));
            Assert.That((allowed - new ValueRange(8000, 8099)).ToString(), Is.EqualTo("22,80,443,8100"));

            // the operands are never changed
            Assert.That(allowed.ToString(), Is.EqualTo("22,80,443,8000-8100"));
            Assert.That(requested.ToString(), Is.EqualTo("80,443,3389,8050-8200"));
        }

        [Test]
        public void SetComparisons()
        {
            var allowed = NumericRange.Parse("22,80,443,8000-8100");

            Assert.That(allowed.Overlaps(NumericRange.Parse("1-22")), Is.True);
            Assert.That(allowed.Overlaps(NumericRange.Parse("23-79,81-442")), Is.False);
            Assert.That(NumericRange.Parse("80,443").IsSubsetOf(allowed), Is.True);
            Assert.That(NumericRange.Parse("80,444").IsSubsetOf(allowed), Is.False);
            Assert.That(allowed.IsSupersetOf(NumericRange.Parse("8000-8100")), Is.True);
            Assert.That(allowed.IsProperSupersetOf(NumericRange.Parse("8000-8100")), Is.True);
            Assert.That(allowed.IsProperSupersetOf(allowed.Clone()), Is.False);
            Assert.That(allowed.IsProperSubsetOf(allowed.Clone()), Is.False);
            Assert.That(allowed.SetEquals(NumericRange.Parse("8000-8100,443,80,22")), Is.True);
            Assert.That(new NumericRange().IsSubsetOf(allowed), Is.True);
            Assert.That(allowed.IsSupersetOf(new NumericRange()), Is.True);
        }

        [Test]
        public void InPlaceOperationsAndCompoundOperators()
        {
            var range = NumericRange.Parse("1-10");
            var alias = range;

            range += new ValueRange(20, 30);
            Assert.That(range.ToString(), Is.EqualTo("1-10,20-30"));
            range -= new ValueRange(5, 25);
            Assert.That(range.ToString(), Is.EqualTo("1-4,26-30"));
            range += NumericRange.Parse("100");
            Assert.That(range.ToString(), Is.EqualTo("1-4,26-30,100"));
            range &= NumericRange.Parse("1-2,28-100");
            Assert.That(range.ToString(), Is.EqualTo("1-2,28-30,100"));
            range ^= NumericRange.Parse("2-3");
            Assert.That(range.ToString(), Is.EqualTo("1,3,28-30,100"));
            range -= NumericRange.Parse("100");
            Assert.That(range.ToString(), Is.EqualTo("1,3,28-30"));

            Assert.That(range, Is.SameAs(alias));

            range.UnionWith(NumericRange.Parse("2"));
            range.IntersectWith(NumericRange.Parse("1-28"));
            range.ExceptWith(NumericRange.Parse("1"));
            range.SymmetricExceptWith(NumericRange.Parse("1,28"));
            Assert.That(range.ToString(), Is.EqualTo("1-3"));
        }

        [Test]
        public void SetOperationsWithItself()
        {
            var range = NumericRange.Parse("1-5,10");

            Assert.That((range + range).ToString(), Is.EqualTo("1-5,10"));
            Assert.That((range & range).ToString(), Is.EqualTo("1-5,10"));
            Assert.That((range - range).IsEmpty, Is.True);
            Assert.That((range ^ range).IsEmpty, Is.True);
            Assert.That(range.Overlaps(range), Is.True);
            Assert.That(range.IsSubsetOf(range), Is.True);
            Assert.That(range.IsProperSubsetOf(range), Is.False);
            Assert.That(new NumericRange().Overlaps(new NumericRange()), Is.False);

            range.UnionWith(range);
            range.IntersectWith(range);
            Assert.That(range.ToString(), Is.EqualTo("1-5,10"));

            range.SymmetricExceptWith(range);
            Assert.That(range.IsEmpty, Is.True);

            range = NumericRange.Parse("1-5,10");
            range.ExceptWith(range);
            Assert.That(range.IsEmpty, Is.True);
        }

        [Test]
        public void SetOperationsRejectNull()
        {
            var range = NumericRange.Parse("1-5");
            NumericRange missing = null!;

            Assert.Throws<ArgumentNullException>(() => range.UnionWith(missing));
            Assert.Throws<ArgumentNullException>(() => range.IntersectWith(missing));
            Assert.Throws<ArgumentNullException>(() => range.ExceptWith(missing));
            Assert.Throws<ArgumentNullException>(() => range.SymmetricExceptWith(missing));
            Assert.Throws<ArgumentNullException>(() => range.Overlaps(missing));
            Assert.Throws<ArgumentNullException>(() => range.IsSubsetOf(missing));
            Assert.Throws<ArgumentNullException>(() => range.IsSupersetOf(missing));
            Assert.Throws<ArgumentNullException>(() => range.IsProperSubsetOf(missing));
            Assert.Throws<ArgumentNullException>(() => range.IsProperSupersetOf(missing));
            Assert.Throws<ArgumentNullException>(() => range.SetEquals(missing));
            Assert.Throws<ArgumentNullException>(() => _ = range + missing);
            Assert.Throws<ArgumentNullException>(() => _ = missing + range);
            Assert.Throws<ArgumentNullException>(() => _ = range - missing);
            Assert.Throws<ArgumentNullException>(() => _ = missing - range);
            Assert.Throws<ArgumentNullException>(() => _ = range & missing);
            Assert.Throws<ArgumentNullException>(() => _ = missing & range);
            Assert.Throws<ArgumentNullException>(() => _ = range ^ missing);
            Assert.Throws<ArgumentNullException>(() => _ = missing ^ range);
            Assert.Throws<ArgumentNullException>(() => _ = new NumericRange(missing));
            Assert.Throws<ArgumentNullException>(() => _ = new NumericRange((IEnumerable<ulong>)null!));
            Assert.Throws<ArgumentNullException>(() => _ = new NumericRange((IEnumerable<ValueRange>)null!));
        }

        [Test]
        public void Equality()
        {
            var first = NumericRange.Parse("1-5,10");
            var second = NumericRange.Parse("10,1-5");
            var different = NumericRange.Parse("1-5");
            NumericRange? missing = null;

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first == second, Is.True);
            Assert.That(first != different, Is.True);
            Assert.That(first.Equals(different), Is.False);
            Assert.That(first.Equals(missing), Is.False);
            Assert.That(first == missing, Is.False);
            Assert.That(missing == null, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(new HashSet<NumericRange> { first, second, different }, Has.Count.EqualTo(2));

            var built = new NumericRange();
            built.Add(3);
            built.Add(1);
            built.Add(2);
            Assert.That(built, Is.EqualTo(NumericRange.Parse("1-3")));
        }

        [Test]
        public void CopiesAreIndependent()
        {
            var original = NumericRange.Parse("1-5");
            var copy = new NumericRange(original);
            var clone = original.Clone();

            copy.Add(10);
            clone.Remove(1);
            original.Add(20);

            Assert.That(original.ToString(), Is.EqualTo("1-5,20"));
            Assert.That(copy.ToString(), Is.EqualTo("1-5,10"));
            Assert.That(clone.ToString(), Is.EqualTo("2-5"));
            Assert.That(copy.ValueCount, Is.EqualTo((UInt128)6));
            Assert.That(clone.ValueCount, Is.EqualTo((UInt128)4));
        }

        [Test]
        public void Construction()
        {
            Assert.That(new NumericRange(new ulong[] { 5, 1, 2, 3, 10, 2 }).ToString(), Is.EqualTo("1-3,5,10"));
            Assert.That(new NumericRange(new[] { new ValueRange(10, 20), new ValueRange(5, 12) }).ToString(), Is.EqualTo("5-20"));

            var initialized = new NumericRange { 80, 443, { 8000, 8100 }, new ValueRange(22) };
            Assert.That(initialized.ToString(), Is.EqualTo("22,80,443,8000-8100"));

            NumericRange expression = [80, 443, new ValueRange(8000, 8100)];
            Assert.That(expression.ToString(), Is.EqualTo("80,443,8000-8100"));
        }

        [Test]
        public void EnumeratesRangesNotValues()
        {
            var range = NumericRange.Parse("40-70,90");
            var expected = new[] { new ValueRange(40, 70), new ValueRange(90) };

            Assert.That(range.ToArray(), Is.EqualTo(expected));
            Assert.That(range.Ranges().ToArray(), Is.EqualTo(expected));
            Assert.That(range.Ranges(), Is.Not.InstanceOf<ICollection<ValueRange>>());

            var starts = new List<ulong>();
            foreach (var (start, _) in range)
                starts.Add(start);

            Assert.That(starts, Is.EqualTo(new ulong[] { 40, 90 }));
            Assert.That(range.Values().Count(), Is.EqualTo(32));
        }

        [Test]
        public void ChangingWhileEnumeratingThrows()
        {
            var range = NumericRange.Parse("1,3,5");

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var valueRange in range)
                    range.Add(valueRange.End + 10);
            });
        }

        [Test]
        public void ParseIgnoresWhitespaceAndOrder()
        {
            Assert.That(NumericRange.Parse(" 80 , 443 ,8000 - 8100\t").ToString(), Is.EqualTo("80,443,8000-8100"));
            Assert.That(NumericRange.Parse("300-700,40-70,50-60,71").ToString(), Is.EqualTo("40-71,300-700"));
            Assert.That(NumericRange.Parse("").IsEmpty, Is.True);
            Assert.That(NumericRange.Parse("   ").IsEmpty, Is.True);
            Assert.That(NumericRange.Parse("80,443".AsSpan()).ToString(), Is.EqualTo("80,443"));
        }

        [Test]
        public void TryParse()
        {
            Assert.That(NumericRange.TryParse("80, 443", out var parsed), Is.True);
            Assert.That(parsed!.ToString(), Is.EqualTo("80,443"));

            Assert.That(NumericRange.TryParse((string?)null, out parsed), Is.False);
            Assert.That(parsed, Is.Null);

            Assert.That(NumericRange.TryParse("1-2-3".AsSpan(), out parsed), Is.False);
            Assert.That(parsed, Is.Null);
        }

        [TestCase("80,44x3", "Unexpected character 'x' at position 5")]
        [TestCase("80, 1-", "Expected a number at position 6, but the text ended")]
        [TestCase("80,,1", "Expected a number at position 3, but found ','")]
        [TestCase("80,18446744073709551616", "The value 18446744073709551616 at position 3 is larger than 18446744073709551615")]
        [TestCase("1, 9-5", "The range 9-5 at position 3 ends before it starts")]
        public void ParseErrorsDescribeTheProblem(string text, string message)
        {
            var exception = Assert.Throws<FormatException>(() => NumericRange.Parse(text));
            Assert.That(exception!.Message, Does.StartWith(message));
        }

        [Test]
        public void ParsesThroughGenericInterfaces()
        {
            Assert.That(ParseAs<NumericRange>("80,443").ToString(), Is.EqualTo("80,443"));
            Assert.That(ParseSpanAs<NumericRange>("80,443").ToString(), Is.EqualTo("80,443"));
            Assert.That(TryParseAs<NumericRange>("80,443", out var parsed), Is.True);
            Assert.That(parsed!.ToString(), Is.EqualTo("80,443"));
            Assert.That(TryParseAs<NumericRange>("x", out _), Is.False);
        }

        private static T ParseAs<T>(string text) where T : IParsable<T>
        {
            return T.Parse(text, null);
        }

        private static T ParseSpanAs<T>(string text) where T : ISpanParsable<T>
        {
            return T.Parse(text.AsSpan(), null);
        }

        private static bool TryParseAs<T>(string text, out T? result) where T : IParsable<T>
        {
            return T.TryParse(text, null, out result);
        }

        private record Settings(NumericRange AllowedPorts);

        [Test]
        public void JsonUsesTheTextForm()
        {
            Assert.That(JsonSerializer.Serialize(NumericRange.Parse("80,443,8000-8100")), Is.EqualTo("\"80,443,8000-8100\""));
            Assert.That(JsonSerializer.Deserialize<NumericRange>("\"80, 443\"")!.ToString(), Is.EqualTo("80,443"));
            Assert.That(JsonSerializer.Deserialize<NumericRange?>("null"), Is.Null);

            var json = JsonSerializer.Serialize(new Settings(NumericRange.Parse("22,80")));
            Assert.That(json, Is.EqualTo("{\"AllowedPorts\":\"22,80\"}"));
            Assert.That(JsonSerializer.Deserialize<Settings>(json)!.AllowedPorts.ToString(), Is.EqualTo("22,80"));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NumericRange>("\"80,x\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NumericRange>("[1,2]"));
        }

        [Test]
        public void ValueRangeJsonRoundTrips()
        {
            var json = JsonSerializer.Serialize(new ValueRange(8000, 8100));

            Assert.That(json, Is.EqualTo("{\"Start\":8000,\"End\":8100}"));
            Assert.That(JsonSerializer.Deserialize<ValueRange>(json), Is.EqualTo(new ValueRange(8000, 8100)));
        }

        [Test]
        public void TypeConverterUsesTheTextForm()
        {
            var converter = TypeDescriptor.GetConverter(typeof(NumericRange));

            Assert.That(converter, Is.InstanceOf<NumericRangeTypeConverter>());
            Assert.That(converter.CanConvertFrom(typeof(string)), Is.True);
            Assert.That(converter.CanConvertTo(typeof(string)), Is.True);
            Assert.That(converter.ConvertFromInvariantString("80, 443"), Is.EqualTo(NumericRange.Parse("80,443")));
            Assert.That(converter.ConvertToInvariantString(NumericRange.Parse("80,443")), Is.EqualTo("80,443"));
            Assert.Throws<FormatException>(() => converter.ConvertFromInvariantString("x"));
        }

        // Builds random pairs of sets over a small domain and checks every set operation and query against plain sets of values.
        [Test]
        public void RandomSetOperationsMatchModel()
        {
            const int domainSize = 40;
            var random = new Random(20261023);

            for (var iteration = 0; iteration < 400; iteration++)
            {
                var (first, firstModel) = BuildRandomSet(random, domainSize);
                var (second, secondModel) = BuildRandomSet(random, domainSize);
                var context = $"first = {first}, second = {second}";

                Assert.That((first & second).ToString(), Is.EqualTo(RangeModel.ToRangeString(firstModel.Intersect(secondModel))), context);
                Assert.That((first + second).ToString(), Is.EqualTo(RangeModel.ToRangeString(firstModel.Union(secondModel))), context);
                Assert.That((first - second).ToString(), Is.EqualTo(RangeModel.ToRangeString(firstModel.Except(secondModel))), context);
                Assert.That((first ^ second).ToString(), Is.EqualTo(RangeModel.ToRangeString(firstModel.Except(secondModel).Union(secondModel.Except(firstModel)))), context);

                var intersected = first.Clone();
                intersected.IntersectWith(second);
                Assert.That(intersected, Is.EqualTo(first & second), context);

                var excepted = first.Clone();
                excepted.ExceptWith(second);
                Assert.That(excepted, Is.EqualTo(first - second), context);

                var symmetric = first.Clone();
                symmetric.SymmetricExceptWith(second);
                Assert.That(symmetric, Is.EqualTo(first ^ second), context);

                Assert.That(first.Overlaps(second), Is.EqualTo(firstModel.Overlaps(secondModel)), context);
                Assert.That(first.IsSubsetOf(second), Is.EqualTo(firstModel.IsSubsetOf(secondModel)), context);
                Assert.That(first.IsSupersetOf(second), Is.EqualTo(firstModel.IsSupersetOf(secondModel)), context);
                Assert.That(first.IsProperSubsetOf(second), Is.EqualTo(firstModel.IsProperSubsetOf(secondModel)), context);
                Assert.That(first.IsProperSupersetOf(second), Is.EqualTo(firstModel.IsProperSupersetOf(secondModel)), context);
                Assert.That(first.SetEquals(second), Is.EqualTo(firstModel.SetEquals(secondModel)), context);
                Assert.That(first == second, Is.EqualTo(firstModel.SetEquals(secondModel)), context);

                Assert.That(first.ValueCount, Is.EqualTo((UInt128)firstModel.Count), context);
                Assert.That(first.Min, Is.EqualTo(firstModel.Count == 0 ? null : firstModel.Min), context);
                Assert.That(first.Max, Is.EqualTo(firstModel.Count == 0 ? null : firstModel.Max), context);
                Assert.That(NumericRange.Parse(first.ToString()), Is.EqualTo(first), context);

                var startValue = (ulong)random.Next(domainSize + 2);
                var endValue = startValue + (ulong)random.Next(domainSize + 2 - (int)startValue);
                var span = new SortedSet<ulong>();
                for (var value = startValue; value <= endValue; value++)
                    span.Add(value);

                var spanContext = $"{context}, span = {startValue}-{endValue}";
                Assert.That(first.Contains(startValue, endValue), Is.EqualTo(span.IsSubsetOf(firstModel)), spanContext);
                Assert.That(first.Overlaps(startValue, endValue), Is.EqualTo(span.Overlaps(firstModel)), spanContext);
                Assert.That(first.Complement(startValue, endValue).ToString(), Is.EqualTo(RangeModel.ToRangeString(span.Except(firstModel))), spanContext);

                for (ulong value = 0; value <= domainSize + 1; value++)
                {
                    var found = first.TryGetRange(value, out var range);
                    Assert.That(found, Is.EqualTo(firstModel.Contains(value)), $"TryGetRange({value}) with {context}");
                    if (found)
                        Assert.That(first.Ranges(), Does.Contain(range), $"TryGetRange({value}) with {context}");
                }
            }
        }

        // Applies random additions and removals, checking that Add and Remove report whether anything changed.
        private static (NumericRange Range, SortedSet<ulong> Model) BuildRandomSet(Random random, int domainSize)
        {
            var range = new NumericRange();
            var model = new SortedSet<ulong>();

            for (var operation = random.Next(12); operation > 0; operation--)
            {
                var first = (ulong)random.Next(domainSize);
                var second = (ulong)random.Next(domainSize);
                var startValue = Math.Min(first, second);
                var endValue = Math.Max(first, second);
                var countBefore = model.Count;

                bool changed;
                if (random.Next(3) > 0)
                {
                    changed = range.Add(startValue, endValue);
                    for (var value = startValue; value <= endValue; value++)
                        model.Add(value);
                }
                else
                {
                    changed = range.Remove(startValue, endValue);
                    for (var value = startValue; value <= endValue; value++)
                        model.Remove(value);
                }

                Assert.That(changed, Is.EqualTo(model.Count != countBefore), $"{range} after change {startValue}-{endValue}");
            }

            return (range, model);
        }
    }
}
