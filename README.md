# Bitvantage.NumericRange
Parse and manipulate huge lists of numeric ranges such as 40-70,90,95,300-700,5000.

A `NumericRange` is a set of `ulong` values stored as sorted, non-overlapping ranges. Looking up, adding and removing values takes O(log n) time, where n is the number of ranges, not the number of values, so a set such as `0-18446744073709551615` costs no more than a single value. An addition or removal that merges or removes several existing ranges also pays a small cost for each of them.

## Installing via NuGet Package Manager
```
PM> NuGet\Install-Package Bitvantage.NumericRange
```

## Quick Start
```csharp
var range = NumericRange.Parse("40-70,90,95,300-700,702,5000");
range.Add(701);							// 40-70,90,95,300-702,5000
range.Add(1000,2000);					// 40-70,90,95,300-702,1000-2000,5000
range.Remove(80,100);					// 40-70,300-702,1000-2000,5000
range.Remove(1500);						// 40-70,300-702,1000-1499,1501-2000,5000

range.Contains(500);					// true
range.Contains(800);					// false
range.Contains(1000, 1499);				// true, every value in 1000-1499 is in the set
range.Overlaps(700, 900);				// true, at least one value in 700-900 is in the set

Console.WriteLine(range.ToString());	// 40-70,300-702,1000-1499,1501-2000,5000
```

`Add` and `Remove` return `true` when the set changed.

## Ranges and counts
Enumerating a set yields its ranges as `ValueRange` values, not its individual values.
```csharp
var range = NumericRange.Parse("40-70,90,300-700");

foreach (var (start, end) in range)
	Console.WriteLine($"{start} to {end}");

range.RangeCount;						// 3
range.ValueCount;						// 433, a UInt128 because 0-18446744073709551615 holds 2^64 values
range.Min;								// 40, or null when the set is empty
range.Max;								// 700
range.TryGetRange(500, out var block);	// true, block is 300-700
range.Values();							// 40, 41, 42, ... check ValueCount before materializing this
```

## Set operations
Two sets can be combined and compared with the same method names as `HashSet<T>`, or with operators.
```csharp
var allowed = NumericRange.Parse("22,80,443,8000-8100");
var requested = NumericRange.Parse("80,443,3389,8050-8200");

var granted = allowed & requested;		// 80,443,8050-8100
var denied = requested - allowed;		// 3389,8101-8200
var either = allowed + requested;		// 22,80,443,3389,8000-8200
var onlyOne = allowed ^ requested;		// 22,3389,8000-8049,8101-8200

allowed.Overlaps(requested);			// true
requested.IsSubsetOf(allowed);			// false
allowed.SetEquals(allowed.Clone());		// true
allowed == NumericRange.Parse("8000-8100,443,80,22");	// true, sets compare by value

var used = NumericRange.Parse("1024-1030,1032");
used.Complement(1024, 65535).Min;		// 1031, the first free port at or above 1024
```
`UnionWith`, `IntersectWith`, `ExceptWith` and `SymmetricExceptWith` change the set in place.

## Operators
`+`, `-`, `&` and `^` return a new set and leave both operands unchanged. `+=`, `-=`, `&=` and `^=` change the set in place without copying it. `+` and `-` accept a value, a `ValueRange` or another set.
```csharp
var ports = NumericRange.Parse("80,443");
var withAdmin = ports + 8443;			// ports: 80,443		withAdmin: 80,443,8443
ports += 22;							// ports: 22,80,443
ports -= 80;							// ports: 22,443
ports += new ValueRange(8000, 8100);	// ports: 22,443,8000-8100
```
In-place `+=`, `-=`, `&=` and `^=` require C# 14 or later. Older compilers treat `x += y` as `x = x + y`, which gives the same result but copies the whole set each time, so it is much slower on large sets.

## Creating sets
```csharp
var fromText = NumericRange.Parse("22, 80, 443, 8000-8100");
var fromValues = new NumericRange(new ulong[] { 443, 80, 22 });
var fromRanges = new NumericRange(new[] { new ValueRange(8000, 8100) });
var initialized = new NumericRange { 22, 80, { 8000, 8100 } };
NumericRange expression = [22, 80, new ValueRange(8000, 8100)];
var copy = fromText.Clone();
```

## Parsing
Items are separated by commas, and each item is a single value or two values joined by a dash. Whitespace is ignored, items can be in any order and can overlap, and empty text is the empty set. `Parse` throws `FormatException` with the position of the problem, and `TryParse` returns `false` instead. `NumericRange` implements `ISpanParsable<NumericRange>`, so frameworks such as ASP.NET Core minimal APIs can bind it from a query string.

## Serialization and configuration
`NumericRange` reads and writes JSON as a string, and binds from configuration files, with no setup.
```json
{ "Firewall": { "AllowedPorts": "22, 80, 443, 8000-8100" } }
```
```csharp
var settings = configuration.GetSection("Firewall").Get<FirewallSettings>();	// AllowedPorts is a NumericRange
JsonSerializer.Serialize(settings.AllowedPorts);								// "22,80,443,8000-8100"
```

## Native AOT and debugging
The library is declared compatible with Native AOT and trimming, and builds with the AOT and trim analyzers turned on. In an AOT app, serialize with a source-generated `JsonSerializerContext` as usual; the JSON converter is picked up automatically.

Packages include Source Link and a `.snupkg` symbol package, so debuggers can step into the exact source of the version you are using.

## Thread safety
`NumericRange` is not thread-safe. Changing a set while enumerating it throws `InvalidOperationException`.

## Performance
Internally ranges are stored in a red black tree, the search time of red black trees is O(log n). `ValueCount` is tracked as the set changes, so reading it is O(1).
