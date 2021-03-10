/*
MIT License

Copyright (c) 2021 John Lenz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Germinate;
using Xunit;
using AutoFixture;
using FluentAssertions;

namespace GerminateTests
{
  [Draftable]
  public record FFF
  {
    public int III { get; init; }
    public string SSS { get; init; }

    public static FFF operator %(FFF value, Action<IFFFDraft> f) => value.Produce(f);
  }

  [Draftable]
  public record GGG
  {
    public bool BBB { get; init; }
    public ImmutableList<long> LongLst { get; init; }
    public ImmutableArray<FFF> FffArr { get; init; }
    public ImmutableList<FFF> FffLst { get; init; }
    public ImmutableDictionary<string, FFF> FffDict { get; init; }
  }

  public class ImmutableSpec
  {
    private Fixture _fixture;

    public ImmutableSpec()
    {
      _fixture = new Fixture();
      _fixture.Customizations.Add(new ImmutableSpecimenBuilder());
    }

    [Fact]
    public void LeavesImmutableUnchanged()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft => { }).Should().BeSameAs(g);

      var g2 = g.Produce(draft => draft.BBB = !draft.BBB);
      g2.Should().Be(g with { BBB = !g.BBB });
      g2.LongLst.Should().BeSameAs(g.LongLst);
      g2.FffLst.Should().BeSameAs(g.FffLst);
    }

    [Fact]
    public void LeavesImmutableUnchangedWhenReading()
    {
      var g = _fixture.Create<GGG>();

      var g2 = g.Produce(draft => draft.BBB = draft.LongLst.Count > 0 && draft.LongLst[0] > 0);

      g2.Should().Be(g with { BBB = true });
      g2.LongLst.Should().BeSameAs(g.LongLst);
      g2.FffLst.Should().BeSameAs(g.FffLst);
    }

    [Fact]
    public void UpdatesPrimList()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft =>
      {
        draft.LongLst[0] += 5;
        draft.LongLst.Add(1);
        draft.LongLst.Add(2);
      })
      .Should().BeEquivalentTo(
        g with { LongLst = g.LongLst.SetItem(0, g.LongLst[0] + 5).AddRange(new long[] { 1, 2 }) },
        options => options.ComparingByMembers<GGG>() // ImmutableList doesn't implement equality check by value
      );
    }

    [Fact]
    public void UpdatesNonNullFffArr()
    {
      var g = _fixture.Create<GGG>();
      var f = _fixture.Create<FFF>();

      g.Produce(draft => draft.FffArr[0] = f)
      .Should().BeEquivalentTo(
        g with { FffArr = (new[] { f }).Concat(g.FffArr.Skip(1)).ToImmutableArray() },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void UpdatesNullFffArr()
    {
      var g = _fixture.Create<GGG>() with { FffArr = default(ImmutableArray<FFF>) };
      var f = _fixture.Create<FFF>();

      g.Produce(draft => draft.FffArr.Add(f))
      .Should().BeEquivalentTo(
        g with { FffArr = ImmutableArray.Create(f) },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void AdjustsFffArr()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft => draft.FffArr.AdjustAll(fd => fd.III += 20))
      .Should().BeEquivalentTo(
        g with { FffArr = g.FffArr.Select(f => f with { III = f.III + 20 }).ToImmutableArray() },
        options => options.ComparingByMembers<GGG>()
      );

      g.Produce(draft => draft.FffArr.AdjustAll((fd, idx) => fd.III += 30 + idx))
      .Should().BeEquivalentTo(
        g with { FffArr = g.FffArr.Select((f, idx) => f with { III = f.III + 30 + idx }).ToImmutableArray() },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void UpdatesFFFList()
    {
      var g = _fixture.Create<GGG>();
      var f = _fixture.Create<FFF>();

      g.Produce(draft =>
      {
        draft.FffLst[0] %= draft => draft.III += 22;
        draft.FffLst.Add(f);
      })
      .Should().BeEquivalentTo(
        g with { FffLst = g.FffLst.SetItem(0, g.FffLst[0] with { III = g.FffLst[0].III + 22 }).Add(f) },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void AddsToNullFFFList()
    {
      var g = _fixture.Create<GGG>() with { FffLst = null };
      var f = _fixture.Create<FFF>();

      g.Produce(draft =>
      {
        draft.FffLst.Add(f);
      })
      .Should().BeEquivalentTo(
        g with { FffLst = ImmutableList.Create(f) },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void AdjustsFFFList()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft =>
      {
        draft.FffLst.AdjustAll(fd =>
        {
          fd.III += 50;
        });
      })
      .Should().BeEquivalentTo(
        g with { FffLst = g.FffLst.Select(f => f with { III = f.III + 50 }).ToImmutableList() },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void AdjustsFFFIndexedList()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft =>
      {
        draft.FffLst.AdjustAll((fd, idx) =>
        {
          fd.III += 10 + idx;
        });
      })
      .Should().BeEquivalentTo(
        g with { FffLst = g.FffLst.Select((f, idx) => f with { III = f.III + 10 + idx }).ToImmutableList() },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void UpdatesFFFDict()
    {
      var g = _fixture.Create<GGG>();
      var newF = _fixture.Create<FFF>();

      g.Produce(draft =>
      {
        draft.FffDict["Hello"] = newF;
      })
      .Should().BeEquivalentTo(
        g with { FffDict = g.FffDict.Add("Hello", newF) },
        options => options.ComparingByMembers<GGG>()
      );
    }

    [Fact]
    public void AdjustsFFFDict()
    {
      var g = _fixture.Create<GGG>();

      g.Produce(draft =>
      {
        draft.FffDict.AdjustAll((k, df) =>
        {
          df.III += 10 + k.Length;
          df.SSS += k;
        });
      })
      .Should()
      .BeEquivalentTo(
        g with
        {
          FffDict = ImmutableDictionary<string, FFF>.Empty.AddRange(
          g.FffDict.Select(k =>
            KeyValuePair.Create(k.Key, k.Value with { III = k.Value.III + 10 + k.Key.Length, SSS = k.Value.SSS + k.Key })
          )
        )
        },
        options => options.ComparingByMembers<GGG>()
      );
    }
  }

  public class ImmutableSpecimenBuilder : AutoFixture.Kernel.ISpecimenBuilder
  {
    public object Create(object request, AutoFixture.Kernel.ISpecimenContext context)
    {
      if (context == null) throw new ArgumentNullException(nameof(context));

      var t = request as Type;
      if (t == null) return new AutoFixture.Kernel.NoSpecimen();

      var args = t.GetGenericArguments();


      if (args.Length == 1 && (t.GetGenericTypeDefinition().FullName.StartsWith("System.Collections.Immutable.ImmutableList") || t.GetGenericTypeDefinition().FullName.StartsWith("System.Collections.Immutable.ImmutableArray")))
      {
        var addRange =
          t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
          .Where(m => m.Name == "AddRange" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
          .FirstOrDefault();

        var list = context.Resolve(addRange.GetParameters()[0].ParameterType);
        var im = t.GetField("Empty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);

        return addRange.Invoke(im, new[] { list });
      }
      else if (args.Length == 2 && t.GetGenericTypeDefinition().FullName.StartsWith("System.Collections.Immutable.ImmutableDictionary"))
      {
        var addRange =
          t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
          .Where(m => m.Name == "AddRange" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
          .FirstOrDefault();

        var dictType = typeof(Dictionary<,>).MakeGenericType(args);
        var dict = context.Resolve(dictType);

        var emptyDict = t.GetField("Empty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
        return addRange.Invoke(emptyDict, new[] { dict });
      }
      else
      {
        return new AutoFixture.Kernel.NoSpecimen();
      }
    }
  }
}