# Germinate

[![NuGet Stats](https://img.shields.io/nuget/v/Germinate.svg)](https://www.nuget.org/packages/Germinate/)

Germinate allows you to modify C# v9 immutable records in a more convienient way.
To obtain a new immutable record, you apply all your changes to a temporary draft.
Once all the mutations are completed, Germinate applys the draft to generate a new
immutable record containing all the mutations. Thus you can interact with your data
by simply modifying it while keeping all the benifits of immutable data.

## Quick Example

```csharp
using System;
using Germinate;

[Draftable]
public record Weather
{
  public int TemperatureC { get; init; }
  public double PressureMB { get; init; }
  public string Wind { get; init; }
}

[Draftable]
public record City
{
  public string Location { get; init; }
  public double Latitude { get; init; }
  public double Longitude { get; init; }
  public Weather CurWeather { get; init; }
}

public static class Program
{
  public static void Main()
  {
    var chicago = new City()
    {
      Location = "Chicago",
      Latitude = 41.9,
      Longitude = -87.6,
      CurWeather = new()
      {
        TemperatureC = 10,
        PressureMB = 1023.4,
        Wind = "High",
      }
    };

    // The following Produce function is created by Germinate
    City chicagoTomorrow = chicago.Produce(draft => {
      draft.CurWeather.TemperatureC = 6;
      draft.CurWeather.PressureMB += 10.2;
    });

    Console.WriteLine(chicago.ToString());
    // => City { Location = Chicago, Latitude = 41.9, Longitude = -87.6,
    //           CurWeather = Weather { TemperatureC = 10, PressureMB = 1023.4, Wind = High } }

    Console.WriteLine(chicagoTomorrow.ToString());
    // => City { Location = Chicago, Latitude = 41.9, Longitude = -87.6,
    //           CurWeather = Weather { TemperatureC = 6, PressureMB = 1033.6, Wind = High } }
  }
}
```

## Motivation

Immutable data is great, but once you start nesting multiple immutable records inside each other it becomes difficult to adjust them.
Using `with`, the above example of updating Chicago's weather would look like:

```csharp
var chicagoTomorrow = chicago with { CurWeather = chicago.CurWeather with {
                                      TemperatureC = 6,
                                      PressureMB = chicago.CurWeather.PressureMB + 10.2,
                                   }};
```

These `with` expressions get more and more complex once more of your data becomes immutable, especially once lists and dictionaries
become involved. There have been traditionally two approaches to this: lenses/optics and copy-on-write to draft objects.
Inspired by [Immer](https://immerjs.github.io/immer/docs/introduction), Germinate takes the latter approach. Germinate's copy-on-write
draft technique is to create the following:

```csharp
public interface IWeatherDraft {
  int TemperatureC { get; set; }
  double PressureMB { get; set; }
  string Wind { get; set; }
}

public interface ICityDraft {
  string Location { get; set; }
  double Latitude { get; set; }
  double Longitude { get; set; }
  IWeatherDraft CurWeather { get; }
  IWeatherDraft SetCurWeather(Weather value);
}

public static class Producer {
  public static Weather Produce(this Weather value, Action<IWeatherDraft> f) { ... }
  public static City Produce(this City value, Action<ICityDraft> f) { ... }
}
```

Note how the `init` properties have changed to `set`. The implementation is straightforward (and tedious). The `Produce` function
first creates drafts where the setter for each property tracks that the property has changed, `Produce` then
calls the user-supplied action `f` to update the draft, and finally converts the draft back to a new immutable record.

## Install

Germinate uses C# source code generation which as of .NET 5 is still in preview. Thus, your csproj file must include
`<LangVersion>preview</LangVersion>` and reference Germinate using the Analyzer output type.

```xml
<PropertyGroup>
  <TargetFramework>net5.0</TargetFramework>
  <LangVersion>preview</LangVersion>
</PropertyGroup>

<ItemGroup>
  <!-- TODO: replace Germinate version with latest from Nuget -->
  <PackageReference Include="Germinate" Version="*" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## The Germinate.DraftableAttribute

For each record with the `Germinate.DraftableAttribute`, Germinate will generate an interface and an extension method `Produce`.

The interface will be named `I<RecordName>Draft` and will be placed into the same namespace as the record
marked with the `Draftable` attribute. For each public property on the record, Germinate will create properties
in the interface with the following rules.

- If the record property's type is itself a draftable record (e.g. the `CurWeather` property of the
  `City` record), the interface will contain two entries. First, a getter with the same property name
  which returns the draft interface for the property type. Second, a property called
  `Set<PropertyName>` which takes the property type. Using the weather example above, the
  `Weather CurWeather {get; init;}` property in the record generates `IWeatherDraft CurWeather {get;}`
  and `IWeatherDraft SetCurWeather(Weather value)` in the draft interface.

- If the record property's type is an [Immutable Collection](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable),
  Germinate adds to the draft interface the `Builder` form of the collection. For example, if the
  record contains a `ImmutableList<int> MyIntList {get; init;}` then Germinate adds
  `ImmutableList<int>.Builder MyIntList {get;}` to the draft interface. The
  [Builder](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablelist-1.builder)
  allows you to use mutable methods to update the list. See the [collections documentation](#collections)
  for more details.

- If the record property's type is neither of the above (basic type such as `int`, a struct, a class, or a
  record not marked as draftable), the property is copied directly into the draft interface with a setter.
  For example, a property `string MyString {get; init;}` in the record will translate to
  `string MyString {get; set;}` in the draft interface.

Germinate will also create a static extension method
`RecordName Produce(this RecordName v, Action<IRecordNameDraft> f)` for each record marked with the
`Draftable` attribute. This will be in the `Germinate` namespace in a static class named after
the assembly, something like `Producer_<assemblyName>`. There is a single static class which contains
all the `Produce` extension methods.

Finally, Germinate generates the implementation of the draftable interfaces inside the
`Germinate.Internal` namespace. These classes are public so that if Germinte is used in multiple
assemblies, Germinate can generate references to the drafts of records in dependent assemblies. These
implementations are carefully implemented with copy-on-write, and delay creating a new draftable
instance until the getter is actually accessed. (That is, if the `CurWeather` property on the city
is never accessed in the action, no weather draft object will be created and the newly returned city
will use the original weather record object.) Despite these being public, they should never be used
directly.

For example, consider the following record:

```csharp
namespace MyNameSpace {
  [Germinate.Draftable]
  public record MyRecord {
    public Weather CurWeather { get; init; }
    public ImmutableList<double> DoubleLst { get; init; }
    public int SomeInt { get; init; }
    public MyClass SomeClass { get; init; }
  }
}
```

Assuming `Weather` is a record marked with the draftable attribute, Germinate produces

```csharp
namespace MyNameSpace {
  public interface IMyRecordDraft {
    public IWeatherDraft CurWeather { get; }
    public IWeatherDraft SetWeather(Weather w);
    public ImmutableList<double>.Builder DoubleLst { get; }
    public int SomeInt { get; set; }
    public MyClass SomeClass { get; set; }
  }
}
namespace Germinate {
  public static class Producer_AssemblyName {
    public static MyRecord Produce(this MyRecord val, Action<IMyRecordDraft> f)
    {
      // make new instance which implements IMyRecordDraft with values initially coming from val
      // call f
      // convert result to new MyRecord
    }
  }
}
```

## Collections

When working with immutable records, they should only contain only immutable data consiting of base types, other
immutable records, or immutable collections. There are two possibilities:
[System.Collections.Immutable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable)
([NuGet](https://www.nuget.org/packages/System.Collections.Immutable)) or just using `IReadOnlyList`/
`IReadOnlyDictionary`.

The prefered approach to collections is the [System.Collections.Immutable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable)
([NuGet](https://www.nuget.org/packages/System.Collections.Immutable)) library.

```csharp
[Draftable]
public record SomeNumbers {
  public ImmutableList<int> Numbers { get; init; }
}
```

Germinate will translate immutable collections to their `Builder` version in the draft interface, allowing you to
make adjustments to the immutable list without requiring a full copy. See this
[article](https://docs.microsoft.com/en-us/archive/msdn-magazine/2017/march/net-framework-immutable-collections)
for more information, but essentially the operations applied to the `Builder` will be applied to the original
immutable list to produce a new immutable list, but these two immutable lists will share as much data and memory
as possible.

```csharp
var evens = new SomeNumbers() { Numbers = ImmutableList.Create(2, 4, 6, 8) };
var moreEvens = evens.Produce(draft => {
  draft.Numbers.Add(draft.Numbers[3] + 2);
  draft.Numbers.Add(12);
});
```

`draft.Numbers` has type
[ImmutableList&lt;int&gt;.Builder](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablelist-1.builder) and Germinate
will initialize the builder with the contents of `evens.Numbers`. At the end of the `Produce` function, Germinate calls the
[ToImmutable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablelist-1.builder.toimmutable)
method to convert the builder to a new immutable list to be contained in `moreEvens`.

### IReadOnlyList and IReadOnlyDictionary

If you don't want to use the immutable collections package, you can consider using
`IReadOnlyList` and `IReadOnlyDictionary` directly in the record. Since these are normal non-draftable properties,
Germinate translates them to a getter and a setter in the draft interface. You can then create a new read only list or
dictionary using LINQ. For example,

```csharp
[Draftable] public record SomeNumbers {
  IReadOnlyList<int> Numbers { get; init; }
}
```

can be used as

```csharp
var evens = new SomeNumbers() { Numbers = new[] {2, 4, 6, 8}};
var odds = evens.Produce(d => d.Numbers = d.Numbers.Select(i => i + 1).ToArray());
```

Care must be taken because while the record itself has type `IReadOnlyList`, there is nothing preventing the initialing code from
keeping a reference to the mutable collection. I initially used `IReadOnlyList` but had a hard to track down bug due to this.
In an existing codebase, we introduced some immutable records but still had some existing normal mutable classes. One of those classes
contained a property of a mutable `System.Collections.Generic.List`. This list was copied into a record with a property of type
`IReadOnlyList` so anything accessing the record couldn't change the list. But of course the list was also referenced from the
mutable class and was mutated there, causing a bug because code using the record expected the list to be unchanged. This could have
been mitigated by copying the list using the LINQ `ToArray()` or `ToList()` at the time the record is created so the record gets its
own copy, but there is nothing to enforce this restriction. Due to this, I converted all the immutable records in our project to use
immutable collections.

## Operator Overload

Germinate does not generate anything with operator overloading, but you might consider adding an overload
to the `%` operator in your record. For example,

```csharp
using Germinate;

[Draftable]
public record City
{
  public string Location { get; init; }
  public double Latitude { get; init; }
  public double Longitude { get; init; }
  public Weather CurWeather { get; init; }

  public static City operator %(City c, Action<ICityDraft> f)
    => c.Produce(f);
}
```

This is most useful in the `%=` form as follows.

```csharp
City chicago = new City() { ... }
chicago %= c => c.CurWeather.TemperatureC += 4;
```

It is especially useful when working with collections of records.

```csharp
[Draftable]
public record Country {
  public ImmutableDictionary<string, City> Cities {get; init;}
}
```

could be used as

```csharp
var unitedStates = new Country() { ... }
unitedStates.Produce(draftUS => {
  draftUS.Cities["Chicago"] %= chicago => {
    chicago.CurWeather.TemperatureC += 4;
  };
});
```

Note how `draftUS.Cities` has type
[ImmutableDictionary&lt;string, City&gt;.Builder](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutabledictionary-2.builder)
and so supports get and set with a new value. The `%=` operator on `City` allows adjusting that value directly with a draft action.

## Limitations

- Each draftable record requires a public zero-argument constructor. The `Produce` function created by Germinate uses this constuctor to initalize
  a new copy of the record, setting all drafted properties.

- If a draftable record inherits from another record, the parent record must also be draftable all the way back to the inheritance root. Just make
  sure each record in the inheritance heirarchy is marked with the `[Draftable]` attribute.

- Using the immutable collection `Builder`s, you can't distinguish between a null or empty collection. Germinate produces the `Builder` lazily
  in the draft property getter. If the original collection was null the builder will be created as initially empty.

- When using Germinate in multiple dependent assemblies and you want to mix the records in the same draftable record,
  there must be a single common ancestor assembly that references Germinate. This is because if no depdenent assembly uses
  Germinate, Germinate generates a base class in the `Germinate.Internal` namespace. If two independent assemblies
  reference Germinate, there will be two separate definitions of this base class. In this situation, Germinate could
  still be used separately on the records in the two assemblies, but you couldn't combine them into a single draftable record.

## License

Licensed under the MIT License
