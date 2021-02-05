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
using Germinate;
using Xunit;
using AutoFixture;
using FluentAssertions;

namespace Germinate.Tests
{
  [Draftable]
  public record BBB
  {
    public int III { get; init; }
    public string SSS { get; init; }
  }

  public record CCC
  {
    public bool BBB { get; init; }
    public TimeSpan Time { get; init; }
  }

  [Draftable]
  public record DDD
  {
    public long LLL { get; init; }
    public BBB B { get; init; }
    public CCC C { get; init; }
  }

  public class NestedSpec
  {
    [Fact]
    public void NoChangeOnEmptyDraft()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();

      d.Produce(draft => { }).Should().BeSameAs(d);
    }

    [Fact]
    public void NoChangeToBBBWhenJustChangingDDDProps()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();

      var d2 = d.Produce(draft => draft.LLL = 150);

      d2.Should().Be(d with { LLL = 150 });

      d2.B.Should().BeSameAs(d.B);
    }

    [Fact]
    public void ChangesNestedProperty()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();

      d.Produce(draft => draft.B.III += 10)
        .Should().Be(d with { B = d.B with { III = d.B.III + 10 } });

      d.Produce(draft =>
      {
        draft.B.III += 15;
        draft.B.SSS += "hello";
      })
      .Should().Be(d with { B = d.B with { III = d.B.III + 15, SSS = d.B.SSS + "hello" } });
    }

    [Fact]
    public void SetsANewBBB()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();
      var newB = fix.Create<BBB>();

      d.Produce(draft => draft.SetB(newB))
        .Should().Be(d with { B = newB });

      d.Produce(draft =>
      {
        draft.SetB(newB);
        draft.B.III += 20;
      })
        .Should().Be(d with { B = newB with { III = newB.III + 20 } });
    }

    [Fact]
    public void HandlesNull()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>() with { B = null };
      var newB = fix.Create<BBB>();

      d.Produce(draft => { }).Should().BeSameAs(d);

      d.Produce(draft =>
      {
        draft.B.Should().BeNull();
        draft.SetB(newB);
        draft.B.Should().NotBeNull();
        draft.B.SSS += "hello";
      })
      .Should().Be(d with { B = newB with { SSS = newB.SSS + "hello" } });
    }

    [Fact]
    public void SetsNull()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();

      d.Produce(draft => draft.SetB(null))
        .Should().Be(d with { B = null });
    }

    [Fact]
    public void SetsNonDraftable()
    {
      var fix = new Fixture();
      var d = fix.Create<DDD>();
      var c = fix.Create<CCC>();

      d.Produce(draft => draft.C = c)
        .Should().Be(d with { C = c });
    }
  }


}