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
using Germinate;
using Xunit;
using AutoFixture;
using FluentAssertions;

namespace GerminateTests
{
  [Draftable]
  public record QQQ
  {
    public float FFF { get; init; }
  }

  [Draftable]
  public record RRR
  {
    public int III { get; init; }
    public string SSS { get; init; }
    public QQQ QQQ { get; init; }
    public ImmutableList<string> Immm { get; init; }
  }

  [Draftable]
  public record SSS : RRR
  {
    public bool BBB { get; init; }
    public TimeSpan Time { get; init; }
  }

  public class InheritanceSpec
  {
    private Fixture _fixture;

    public InheritanceSpec()
    {
      _fixture = new Fixture();
      _fixture.Customizations.Add(new ImmutableSpecimenBuilder());
    }

    [Fact]
    public void LeavesUnchanged()
    {
      var s = _fixture.Create<SSS>();

      var s2 = s.Produce(draft =>
      {
        // read but don't write draft.Immm
        System.Diagnostics.Debug.WriteLine(draft.Immm.Count.ToString());
      });

      s.Should().BeSameAs(s2);
    }

    [Fact]
    public void AdjustsSSS()
    {
      var s = _fixture.Create<SSS>();
      var q = _fixture.Create<QQQ>();

      var s2 = s.Produce(draft =>
      {
        draft.III += 2;
        draft.SetQQQ(q);
        draft.QQQ.FFF += 10.0f;
        draft.Immm.Add("Hello World");
        draft.BBB = !draft.BBB;
        draft.Time += TimeSpan.FromHours(2);
      });

      s2.Should().BeEquivalentTo(new SSS
      {
        III = s.III + 2,
        SSS = s.SSS,
        QQQ = q with { FFF = q.FFF + 10.0f },
        Immm = s.Immm.Add("Hello World"),
        BBB = !s.BBB,
        Time = s.Time.Add(TimeSpan.FromHours(2))
      }, options => options.ComparingByMembers<SSS>());
    }

    [Fact]
    public void SetsRRR()
    {
      var s = _fixture.Create<SSS>();
      var r = _fixture.Create<RRR>();

      var s2 = s.Produce(draft => draft.SetFromRecord(r));

      s2.Should().BeEquivalentTo(new SSS
      {
        III = r.III,
        SSS = r.SSS,
        QQQ = r.QQQ,
        Immm = r.Immm,
        BBB = s.BBB,
        Time = s.Time
      }, options => options.ComparingByMembers<SSS>());
    }

    [Fact]
    public void SetsSSS()
    {
      var s = _fixture.Create<SSS>();
      var newS = _fixture.Create<SSS>();

      s.Produce(draft => draft.SetFromRecord(newS))
        .Should().BeEquivalentTo(newS, options => options.ComparingByMembers<SSS>());
    }
  }
}