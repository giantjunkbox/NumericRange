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

using Bitvantage;

namespace Test
{
    internal class NumericRangeTest
    {
        [Test]
        public void Add01()
        {
            var range = new NumericRange();
            
            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(25, 40);

            Assert.That(range.ToString(), Is.EqualTo("10-20,25-53"));
        }

        [Test]
        public void Add02()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(13, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-25,27-30,33-53"));
        }

        [Test]
        public void Add03()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(21, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-25,27-30,33-53"));
        }

        [Test]
        public void Add04()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(22, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-20,22-25,27-30,33-53"));
        }

        [Test]
        public void Add05()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(20, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-25,27-30,33-53"));
        }

        [Test]
        public void Add06()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(19, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-25,27-30,33-53"));
        }

        [Test]
        public void Add07()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(10, 25);

            Assert.That(range.ToString(), Is.EqualTo("10-25,27-30,33-53"));
        }

        [Test]
        public void Add08()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);

            range.Add(09, 25);

            Assert.That(range.ToString(), Is.EqualTo("9-25,27-30,33-53"));
        }

        [Test]
        public void Add09()
        {
            var range = new NumericRange();

            range.Add(0);
            range.Add(100);
            range.Add(ulong.MaxValue);

            Assert.That(range.ToString(), Is.EqualTo("0,100,18446744073709551615"));
        }

        [Test]
        public void Add10()
        {
            var range = new NumericRange();

            //range.Add(0);
            //range.Add(100);

            range.Add(ulong.MaxValue);
            range.Add(ulong.MinValue, ulong.MaxValue);

            Assert.That(range.ToString(), Is.EqualTo("0-18446744073709551615"));
        }


        [Test]
        public void Parse01()
        {
            var range = NumericRange.Parse("10,100,1000,10000-2000000");

            Assert.That(range.ToString(), Is.EqualTo("10,100,1000,10000-2000000"));
        }

        [Test]
        public void Contains01()
        {
            var range = new NumericRange();

            range.Add(10, 20);
            range.Add(ulong.MaxValue);
            range.Add(25);
            range.Add(27, 30);
            range.Add(33, 53);


            Assert.That(range.Contains(0), Is.False);
            Assert.That(range.Contains(10), Is.True);
            Assert.That(range.Contains(11), Is.True);
            Assert.That(range.Contains(12), Is.True);
            Assert.That(range.Contains(13), Is.True);
            Assert.That(range.Contains(14), Is.True);
            Assert.That(range.Contains(15), Is.True);
            Assert.That(range.Contains(16), Is.True);
            Assert.That(range.Contains(17), Is.True);
            Assert.That(range.Contains(18), Is.True);
            Assert.That(range.Contains(19), Is.True);
            Assert.That(range.Contains(20), Is.True);
            Assert.That(range.Contains(21), Is.False);
            Assert.That(range.Contains(22), Is.False);
            Assert.That(range.Contains(23), Is.False);
            Assert.That(range.Contains(23), Is.False);
            Assert.That(range.Contains(24), Is.False);
            Assert.That(range.Contains(25), Is.True);
            Assert.That(range.Contains(26), Is.False);
            Assert.That(range.Contains(27), Is.True);
            Assert.That(range.Contains(28), Is.True);
            Assert.That(range.Contains(29), Is.True);
            Assert.That(range.Contains(30), Is.True);
            Assert.That(range.Contains(31), Is.False);
            Assert.That(range.Contains(32), Is.False);
            Assert.That(range.Contains(33), Is.True);
            Assert.That(range.Contains(34), Is.True);
            Assert.That(range.Contains(35), Is.True);
            Assert.That(range.Contains(36), Is.True);
            Assert.That(range.Contains(37), Is.True);
            Assert.That(range.Contains(38), Is.True);
            Assert.That(range.Contains(39), Is.True);
            Assert.That(range.Contains(40), Is.True);
            Assert.That(range.Contains(41), Is.True);
            Assert.That(range.Contains(42), Is.True);
            Assert.That(range.Contains(43), Is.True);
            Assert.That(range.Contains(44), Is.True);
            Assert.That(range.Contains(45), Is.True);
            Assert.That(range.Contains(46), Is.True);
            Assert.That(range.Contains(47), Is.True);
            Assert.That(range.Contains(48), Is.True);
            Assert.That(range.Contains(49), Is.True);
            Assert.That(range.Contains(50), Is.True);
            Assert.That(range.Contains(51), Is.True);
            Assert.That(range.Contains(52), Is.True);
            Assert.That(range.Contains(53), Is.True);
            Assert.That(range.Contains(54), Is.False);
            Assert.That(range.Contains(55), Is.False);
            Assert.That(range.Contains(56), Is.False);
            Assert.That(range.Contains(57), Is.False);
            Assert.That(range.Contains(58), Is.False);
            Assert.That(range.Contains(59), Is.False);
            Assert.That(range.Contains(60), Is.False);
            Assert.That(range.Contains(long.MaxValue), Is.False);
        }

        [Test]
        public void Contains02()
        {
            var range = new NumericRange();

            Assert.That(range.Contains(0), Is.False);
            range.Add(0);
            Assert.That(range.Contains(0), Is.True);

            range = new NumericRange();
            Assert.That(range.Contains(ulong.MaxValue), Is.False);
            range.Add(ulong.MaxValue);
            Assert.That(range.Contains(ulong.MaxValue), Is.True);

            range = new NumericRange();
            range.Add(ulong.MinValue);
            range.Add(100);
            range.Add(ulong.MaxValue);

            Assert.That(range.Contains(ulong.MinValue));
            Assert.That(range.Contains(100));
            Assert.That(range.Contains(ulong.MaxValue));
        }

        [Test]
        public void Remove01()
        {
            NumericRange range;

            // single value
            range = NumericRange.Parse("10,100,500-5000");
            range.Remove(100);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("10,100,500-5000");
            range.Remove(99);
            Assert.That(range.ToString(), Is.EqualTo("10,100,500-5000"));

            range = NumericRange.Parse("10,100,500-5000");
            range.Remove(101);
            Assert.That(range.ToString(), Is.EqualTo("10,100,500-5000"));


            //for (ulong startValue = 99; startValue <= 101; startValue++)
            //for (ulong endValue = 200; endValue <= 201; endValue++)
            //{
            //    range = NumericRange.Parse("10,100,200,500-5000");
            //    range.Remove(startValue, endValue);
            //    Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));
            //}

            //for (ulong startValue = 99; startValue <= 101; startValue++)
            //{
            //    range = NumericRange.Parse("10,100,200,500-5000");
            //    range.Remove(startValue, 199);
            //    Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));
            //}


            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(99, 199);
            Assert.That(range.ToString(), Is.EqualTo("10,200,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(99, 200);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(99, 201);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(100, 199);
            Assert.That(range.ToString(), Is.EqualTo("10,200,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(100, 200);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(100, 201);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(101, 199);
            Assert.That(range.ToString(), Is.EqualTo("10,100,200,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(101, 200);
            Assert.That(range.ToString(), Is.EqualTo("10,100,500-5000"));

            range = NumericRange.Parse("10,100,200,500-5000");
            range.Remove(101, 201);
            Assert.That(range.ToString(), Is.EqualTo("10,100,500-5000"));


            range = NumericRange.Parse("100-200,500-5000");
            range.Remove(0, 300);
            Assert.That(range.ToString(), Is.EqualTo("500-5000"));

            range = NumericRange.Parse("10,100-200,500-5000");
            range.Remove(100, 300);
            Assert.That(range.ToString(), Is.EqualTo("10,500-5000"));

            range = NumericRange.Parse("100-200,500-5000");
            range.Remove(0, 200);
            Assert.That(range.ToString(), Is.EqualTo("500-5000"));

            range = NumericRange.Parse("10,100-200,500-5000");
            range.Remove(150);
            Assert.That(range.ToString(), Is.EqualTo("10,100-149,151-200,500-5000"));

        }

        [TestCase("100-150", 90UL, 120UL, "121-150")]
        [TestCase("100-150", 100UL, 120UL, "121-150")]
        [TestCase("100-150", 120UL, 200UL, "100-119")]
        [TestCase("100-150", 120UL, 150UL, "100-119")]
        [TestCase("100-150", 120UL, 130UL, "100-119,131-150")]
        [TestCase("100-150", 100UL, 150UL, "")]
        [TestCase("100-150", 0UL, 99UL, "100-150")]
        [TestCase("100-150", 151UL, 200UL, "100-150")]
        [TestCase("100-150,200-250,300-350", 120UL, 320UL, "100-119,321-350")]
        [TestCase("100-150,200-250,300-350", 90UL, 360UL, "")]
        [TestCase("100-150,200-250,300-350", 150UL, 300UL, "100-149,301-350")]
        [TestCase("0-100", 0UL, 10UL, "11-100")]
        [TestCase("0-18446744073709551615", 10UL, 18446744073709551615UL, "0-9")]
        [TestCase("0-18446744073709551615", 0UL, 18446744073709551614UL, "18446744073709551615")]
        [TestCase("0-18446744073709551615", 1UL, 18446744073709551614UL, "0,18446744073709551615")]
        public void RemovePartialOverlap(string initial, ulong startValue, ulong endValue, string expected)
        {
            var range = NumericRange.Parse(initial);
            range.Remove(startValue, endValue);
            Assert.That(range.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void AddMergesAdjacentRangeOnTheRight()
        {
            var range = new NumericRange();
            range.Add(21, 30);
            range.Add(10, 20);
            Assert.That(range.ToString(), Is.EqualTo("10-30"));

            range = NumericRange.Parse("10-20,22-30");
            range.Add(21);
            Assert.That(range.ToString(), Is.EqualTo("10-30"));
        }

        [Test]
        public void ReadmeExample()
        {
            var range = NumericRange.Parse("40-70,90,95,300-700,702,5000");
            range.Add(701);
            Assert.That(range.ToString(), Is.EqualTo("40-70,90,95,300-702,5000"));
            range.Add(1000, 2000);
            Assert.That(range.ToString(), Is.EqualTo("40-70,90,95,300-702,1000-2000,5000"));
            range.Remove(80, 100);
            Assert.That(range.ToString(), Is.EqualTo("40-70,300-702,1000-2000,5000"));
            range.Remove(1500);
            Assert.That(range.ToString(), Is.EqualTo("40-70,300-702,1000-1499,1501-2000,5000"));
            Assert.That(range.Contains(500), Is.True);
            Assert.That(range.Contains(800), Is.False);
        }

        [Test]
        public void AddInsideRangeEndingAtMaxValue()
        {
            var range = new NumericRange();
            range.Add(0, ulong.MaxValue);
            range.Add(10);
            range.Add(ulong.MaxValue);
            Assert.That(range.ToString(), Is.EqualTo("0-18446744073709551615"));

            range = new NumericRange();
            range.Add(5, ulong.MaxValue);
            range.Add(0, 4);
            Assert.That(range.ToString(), Is.EqualTo("0-18446744073709551615"));
        }

        [Test]
        public void ReversedRangeIsRejected()
        {
            var range = new NumericRange();
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Add(20, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Remove(20, 10));
            Assert.Throws<FormatException>(() => NumericRange.Parse("70-40"));
            Assert.That(range.ToString(), Is.EqualTo(""));
        }

        [TestCase("18446744073709551616")]
        [TestCase("1-18446744073709551616")]
        [TestCase("\u0663")]
        [TestCase("10 20")]
        [TestCase("1,")]
        [TestCase(",1")]
        [TestCase("1,,2")]
        [TestCase("1-")]
        [TestCase("-1")]
        [TestCase("1-2-3")]
        [TestCase("a")]
        [TestCase("1;2")]
        public void ParseRejectsInvalidInput(string value)
        {
            Assert.Throws<FormatException>(() => NumericRange.Parse(value));
            Assert.That(NumericRange.TryParse(value, out var result), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ParseMaxValue()
        {
            Assert.That(NumericRange.Parse("18446744073709551615").ToString(), Is.EqualTo("18446744073709551615"));
        }

        [Test]
        public void NullArgumentsAreRejected()
        {
            var range = new NumericRange();
            Assert.Throws<ArgumentNullException>(() => range.Add((NumericRange)null!));
            Assert.Throws<ArgumentNullException>(() => range.Remove((NumericRange)null!));
            Assert.Throws<ArgumentNullException>(() => NumericRange.Parse(null!));
        }

        [Test]
        public void PlusReturnsNewRange()
        {
            var range = NumericRange.Parse("1-5");
            var result = range + 10;

            Assert.That(result, Is.Not.SameAs(range));
            Assert.That(result.ToString(), Is.EqualTo("1-5,10"));
            Assert.That(range.ToString(), Is.EqualTo("1-5"));

            // the copy and the original do not share state afterwards
            result.Add(20);
            range.Add(30);
            Assert.That(result.ToString(), Is.EqualTo("1-5,10,20"));
            Assert.That(range.ToString(), Is.EqualTo("1-5,30"));
        }

        [Test]
        public void MinusReturnsNewRange()
        {
            var range = NumericRange.Parse("1-10");
            var result = range - 5;

            Assert.That(result, Is.Not.SameAs(range));
            Assert.That(result.ToString(), Is.EqualTo("1-4,6-10"));
            Assert.That(range.ToString(), Is.EqualTo("1-10"));
        }

        [Test]
        public void ChainedOperatorsLeaveOriginalUnchanged()
        {
            var range = NumericRange.Parse("1-10");
            var result = range + 20 - 5;

            Assert.That(result.ToString(), Is.EqualTo("1-4,6-10,20"));
            Assert.That(range.ToString(), Is.EqualTo("1-10"));
        }

        [Test]
        public void PlusEqualsChangesRangeInPlace()
        {
            var range = NumericRange.Parse("1-5");
            var alias = range;

            range += 6;

            Assert.That(range, Is.SameAs(alias));
            Assert.That(alias.ToString(), Is.EqualTo("1-6"));
        }

        [Test]
        public void MinusEqualsChangesRangeInPlace()
        {
            var range = NumericRange.Parse("1-10");
            var alias = range;

            range -= 5;

            Assert.That(range, Is.SameAs(alias));
            Assert.That(alias.ToString(), Is.EqualTo("1-4,6-10"));
        }

        [Test]
        public void OperatorsRejectNullRange()
        {
            NumericRange range = null!;
            Assert.Throws<ArgumentNullException>(() => _ = range + 5);
            Assert.Throws<ArgumentNullException>(() => _ = range - 5);
        }

        [Test]
        public void AddAndRemoveSelf()
        {
            var range = NumericRange.Parse("1-5,10");
            range.Add(range);
            Assert.That(range.ToString(), Is.EqualTo("1-5,10"));

            range.Remove(range);
            Assert.That(range.ToString(), Is.EqualTo(""));
        }

        [Test]
        public void ValuesAtMaxValue()
        {
            var range = new NumericRange();
            range.Add(ulong.MaxValue - 2, ulong.MaxValue);
            Assert.That(range.Values().Take(10).ToArray(), Is.EqualTo(new[] { ulong.MaxValue - 2, ulong.MaxValue - 1, ulong.MaxValue }));
        }

        // Applies random adds and removes over a small domain and checks the result against a plain set of values.
        [Test]
        public void RandomOperationsMatchModel()
        {
            const int domainSize = 64;
            var random = new Random(20241023);

            for (var iteration = 0; iteration < 300; iteration++)
            {
                var range = new NumericRange();
                var model = new SortedSet<ulong>();
                var history = new List<string>();

                for (var operation = 0; operation < 40; operation++)
                {
                    var first = (ulong)random.Next(domainSize);
                    var second = (ulong)random.Next(domainSize);
                    var startValue = Math.Min(first, second);
                    var endValue = Math.Max(first, second);
                    var isAdd = random.Next(2) == 0;

                    if (isAdd)
                        range.Add(startValue, endValue);
                    else
                        range.Remove(startValue, endValue);

                    for (var value = startValue; value <= endValue; value++)
                        if (isAdd)
                            model.Add(value);
                        else
                            model.Remove(value);

                    history.Add($"{(isAdd ? "Add" : "Remove")}({startValue},{endValue})");
                    var context = string.Join(" ", history);

                    Assert.That(range.ToString(), Is.EqualTo(RangeModel.ToRangeString(model)), context);
                    Assert.That(range.Values().ToArray(), Is.EqualTo(model.ToArray()), context);
                    var allValues = Enumerable.Range(0, domainSize + 1).Select(value => (ulong)value).ToArray();
                    Assert.That(allValues.Where(range.Contains).ToArray(), Is.EqualTo(model.ToArray()), $"Contains() after {context}");
                }
            }
        }
    }
}