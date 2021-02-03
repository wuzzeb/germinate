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
    }
  }
}