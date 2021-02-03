using System;
using System.Linq;
using System.Collections.Generic;
using Germinate;

namespace Germinate.Tests
{
  [Draftable]
  public record Foo
  {
    public int MyInt { get; init; }
    public float MyFloat { get; init; }
  }

  [Draftable]
  public record Bar
  {
    public string MyStr { get; init; }
    public Foo MyFoo { get; init; }
    public IReadOnlyList<Foo> Lst { get; init; }
  }

  public class Program
  {
    public static void Main()
    {
      var f = new Foo()
      {
        MyInt = 212,
        MyFloat = 244.2f,
      };

      Console.WriteLine(f.ToString());
      Console.WriteLine(f.Produce(fd =>
      {
        fd.MyInt = 111;
      }).ToString());

      var b = new Bar()
      {
        MyStr = "Hello",
        MyFoo = f,
        Lst = new[] { f, f.Produce(draft => draft.MyInt = 999) }
      };

      Console.WriteLine(b.ToString());
      Console.WriteLine(string.Join(",", b.Lst.Select(f => f.ToString())));

      var b2 = b.Produce(draft =>
      {
        draft.MyFoo.MyFloat = 6666.2f;
        draft.Lst[1].MyInt = 1567;
      });

      Console.WriteLine(b2.ToString());
      Console.WriteLine(string.Join(",", b2.Lst.Select(f => f.ToString())));

    }
  }
}