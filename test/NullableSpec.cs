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

#nullable enable

namespace Germinate.Tests
{
  [Draftable]
  public record HHH
  {
    public long LLL { get; init; }
    public TimeSpan? Time { get; init; }
  }

  [Draftable]
  public record NNN
  {
    public HHH NonNullHHH { get; init; } = new() { LLL = 10L };
    public HHH? NullHHH { get; init; }
    public Uri? Uri { get; init; } // want to test a normal class marked as nullable
  }

  public class NullableSpec
  {
    [Fact]
    public void SetsNonNullHHH()
    {
      var n = new NNN() { };

      n.NonNullHHH.LLL.Should().Be(10L);

      var n2 = n.Produce(draft =>
      {
        draft.NonNullHHH.LLL += 4;
      });

      n2.NonNullHHH.LLL.Should().Be(14L);
      n2.NullHHH.Should().BeNull();
      n2.Uri.Should().BeNull();
    }

    [Fact]
    public void DraftsAreNull()
    {
      var n = new NNN() { };

      var n2 = n.Produce(draft =>
      {
        draft.NullHHH.Should().BeNull();
        draft.Uri.Should().BeNull();
      });

      n2.NonNullHHH.Should().BeSameAs(n.NonNullHHH);
    }

    [Fact]
    public void SetsToRealValues()
    {
      var fixture = new Fixture();
      var n = new NNN() { };
      var h = fixture.Create<HHH>();

      var n2 = n.Produce(draft =>
      {
        draft.SetNullHHH(h);
        draft.Uri = new Uri("https://github.com");
      });

      n2.NullHHH.Should().Be(h);
      n2.NonNullHHH.Should().BeSameAs(n.NonNullHHH);
      n2.Uri.Should().Be(new Uri("https://github.com"));
    }

    [Fact]
    public void SetsToNull()
    {
      var fixture = new Fixture();
      var h = fixture.Create<HHH>();
      var n = new NNN()
      {
        NullHHH = h,
        Uri = new Uri("https://github.com")
      };

      var n2 = n.Produce(draft =>
      {
        draft.SetNullHHH(null);
        draft.Uri = null;
      });

      n2.NonNullHHH.Should().BeSameAs(n.NonNullHHH);
      n2.NullHHH.Should().BeNull();
      n2.Uri.Should().BeNull();
    }
  }
}