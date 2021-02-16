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

namespace GerminateTests
{
  [Draftable]
  public record AAA
  {
    public int III { get; init; }
    public string SSS { get; init; }
    public bool? NullBool { get; init; }
    public DateTime SomeDate { get; init; }
    public int ReadOnlyIII => III + 15;
  }

  public class BasicSpec
  {
    [Fact]
    public void SameInstanceWithoutDraftChange()
    {
      var fix = new Fixture();
      var a = fix.Create<AAA>();

      a.Produce(draft => { }).Should().BeSameAs(a);
    }

    [Fact]
    public void SetsInt()
    {
      var fix = new Fixture();
      var a = fix.Create<AAA>();

      a.Produce(draft => draft.III += 100)
        .Should().Be(a with { III = a.III + 100 });
    }

    [Fact]
    public void SetsString()
    {
      var fix = new Fixture();
      var a = fix.Create<AAA>();

      a.Produce(draft => draft.SSS = "hello")
        .Should().Be(a with { SSS = "hello" });
    }

    [Fact]
    public void SetsNullBool()
    {
      var fix = new Fixture();
      var a = fix.Create<AAA>();

      a.Produce(draft => draft.NullBool = null)
        .Should().Be(a with { NullBool = null });

      a.Produce(draft => draft.NullBool = !(draft.NullBool ?? true))
        .Should().Be(a with { NullBool = !(a.NullBool ?? true) });
    }

    [Fact]
    public void SetsDate()
    {
      var fix = new Fixture();
      var a = fix.Create<AAA>();

      a.Produce(draft => draft.SomeDate += TimeSpan.FromHours(1))
        .Should().Be(a with { SomeDate = a.SomeDate.AddHours(1) });
    }
  }

}