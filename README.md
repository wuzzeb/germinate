# Germinate

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
  public Weather Weather { get; init; }
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
      Weather = new()
      {
        TemperatureC = 10,
        PressureMB = 1023.4,
        Wind = "High",
      }
    };
    
    // The following Produce function is created by Germinate
    var chicagoTomorrow = chicago.Produce(draft => {
      draft.Weather.TemperatureC = 6;
      draft.Weather.PressureMB = 1020.5;
    });

    Console.WriteLine(chicago.ToString());
    // => City { Location = Chicago, Latitude = 41.9, Longitude = -87.6,
    //           Weather = Weather { TemperatureC = 10, PressureMB = 1023.4, Wind = "High" } }

    Console.WriteLine(chicagoTomorrow.ToString());
    // => City { Location = Chicago, Latitude = 41.9, Longitude = -87.6,
    //           Weather = Weather { TemperatureC = 6, PressureMB = 1020.5, Wind = "High" } }
  }
}
```

## Motivation

Immutable data is great, but once you start nesting multiple immutable records inside each other it becomes difficult to adjust them.
Using `with`, the above example of updating Chicago's weather would look like:

```csharp
var chicagoTomorrow = chicago with { Weather = chicago.Weather with { TemperatureC = 6, PressureMB = 1020.5 }};
```

These `with` expressions get more and more complex once more of your data becomes immutable, especially once lists and dictionaries
become involved. There have been traditionally two approaches to this: lenses/optics and copy-on-write to draft objects.
Inspired by [Immer](https://immerjs.github.io/immer/docs/introduction), Germinate takes the latter approach. Germinate's copy-on-write
draft technique is to create the following:

```csharp
public interface IWeatherDraft {
  int TemperatureC { get; set; }
  double PressureMB { get; set; }
}

public interface ICityDraft {
  string Location { get; set; }
  double Latitude { get; set; }
  double Longitude { get; set; }
  IWeatherDraft Weather { get; }
  ICityDraft SetWeather(Weather value);
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

## License

Licensed under the MIT License
