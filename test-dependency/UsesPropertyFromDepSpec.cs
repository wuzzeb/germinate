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

namespace GerminateTestDependency
{
  [Draftable]
  public record TTT
  {
    public string SSS { get; init; }
    public GerminateTests.DDD DDD { get; init; }
  }

  public class UsesPropertyFromDepSpec
  {
    [Fact]
    public void NoChangeOnEmpty()
    {
      var fix = new Fixture();
      var t = fix.Create<TTT>();

      t.Produce(draft => { }).Should().BeSameAs(t);
    }

    [Fact]
    public void ChangesNestedProperty()
    {
      var fix = new Fixture();
      var t = fix.Create<TTT>();
      var c = fix.Create<GerminateTests.CCC>();

      var t2 = t.Produce(draft =>
      {
        draft.DDD.B.III += 44;
        draft.DDD.C = c;
      });

      t2.Should().Be(t with
      {
        DDD = t.DDD with
        {
          B = t.DDD.B with { III = t.DDD.B.III + 44 },
          C = c
        }
      });
    }
  }
}