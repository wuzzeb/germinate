using System;
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
        MyFoo = f
      };

      Console.WriteLine(b.ToString());
      Console.WriteLine(b.Produce(draft =>
      {
        draft.MyFoo.MyFloat = 6666.2f;
      }));
    }
  }
}