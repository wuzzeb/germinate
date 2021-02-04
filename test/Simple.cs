using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Germinate;

namespace Germinate.Tests
{
  [Draftable]
  public record Foo
  {
    public int MyInt { get; init; }
    public float MyFloat { get; init; }
    public IReadOnlyList<string> StrLst { get; init; }

    public ImmutableList<int> IntLst { get; init; }

    public static Foo operator %(Foo value, Action<IFooDraft> f) => value.Produce(f);
  }

  [Draftable]
  public record Bar
  {
    public string MyStr { get; init; }
    public Foo MyFoo { get; init; }
    //public IReadOnlyList<Foo> Lst { get; init; }
    public ImmutableList<Foo> Lst { get; init; }

    public static Bar operator %(Bar value, Action<IBarDraft> f) => value.Produce(f);
  }

  public class Program
  {
    public static void Main()
    {
      var f = new Foo()
      {
        MyInt = 212,
        MyFloat = 244.2f,
        StrLst = new[] { "aa", "bb" },
      };

      Console.WriteLine(f.ToString());
      Console.WriteLine(string.Join(",", f.StrLst));

      f %= draft => draft.IntLst.Add(55);
      Console.WriteLine(f.ToString());
      Console.WriteLine(string.Join(",", f.IntLst));

      Console.WriteLine(f.ToString());
      Console.WriteLine(string.Join(",", f.StrLst));

      var f2 = f.Produce(fd =>
      {
        fd.MyInt = 111;
        fd.StrLst = fd.StrLst.Select(s => s + "cc").ToList();
      });

      Console.WriteLine(f2.ToString());
      Console.WriteLine(string.Join(",", f2.StrLst));

      f2 %= draft => draft.MyInt = 6666666;

      Console.WriteLine(f2.ToString());
      Console.WriteLine(string.Join(",", f2.StrLst));

      var b = new Bar()
      {
        MyStr = "Hello",
        MyFoo = f,
        Lst = ImmutableList<Foo>.Empty.AddRange(new[] { f, f.Produce(draft => draft.MyInt = 999) })
      };

      Console.WriteLine(b.ToString());
      Console.WriteLine(string.Join(",", b.Lst.Select(f => f.ToString())));

      var b2 = b.Produce(draft =>
      {
        draft.MyFoo.MyFloat = 6666.2f;
        draft.Lst[1] %= draft => draft.MyInt = 1567;
      });

      Console.WriteLine(b2.ToString());
      Console.WriteLine(string.Join(",", b2.Lst.Select(f => f.ToString())));

    }
  }
}