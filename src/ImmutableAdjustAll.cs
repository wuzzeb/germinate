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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Germinate.Generator
{
  public static class ImmutableAdjustAll
  {
    public static void EmitAdjustAll(DraftableRecord r, StringBuilder output)
    {
      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableArray))
      {
        EmitArrayAdjustAll(r, output);
      }

      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableHashSet))
      {
        EmitOneTypeArg("System.Collections.Immutable.ImmutableHashSet", includeIndexed: false, r, output);
      }

      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableList))
      {
        EmitOneTypeArg("System.Collections.Immutable.ImmutableList", includeIndexed: true, r, output);
      }

      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableSortedSet))
      {
        EmitOneTypeArg("System.Collections.Immutable.ImmutableSortedSet", includeIndexed: true, r, output);
      }

      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableDictionary))
      {
        EmitTwoTypeArg("System.Collections.Immutable.ImmutableDictionary", r, output);
      }

      if (r.UsedInImmutableCollections.HasFlag(ImmutableCollectionType.ImmutableSortedDictionary))
      {
        EmitTwoTypeArg("System.Collections.Immutable.ImmutableSortedDictionary", r, output);
      }
    }

    private static void EmitArrayAdjustAll(DraftableRecord r, StringBuilder output)
    {
      output.AppendLine($"  public static void AdjustAll(this System.Collections.Immutable.ImmutableArray<{r.FullyQualifiedRecordName}>.Builder b, System.Action<{r.FullyQualifiedInterfaceName}> f)");
      output.AppendLine("  {");
      output.AppendLine("    for (int i = 0; i < b.Count; i++) {");
      output.AppendLine("      b[i] = b[i].Produce(f);");
      output.AppendLine("    }");
      output.AppendLine("  }");

      output.AppendLine($"  public static void AdjustAll(this System.Collections.Immutable.ImmutableArray<{r.FullyQualifiedRecordName}>.Builder b, System.Action<{r.FullyQualifiedInterfaceName}, int> f)");
      output.AppendLine("  {");
      output.AppendLine("    for (int i = 0; i < b.Count; i++) {");
      output.AppendLine("      b[i] = b[i].Produce(x => f(x, i));");
      output.AppendLine("    }");
      output.AppendLine("  }");
    }

    private static void EmitOneTypeArg(string immutableType, bool includeIndexed, DraftableRecord r, StringBuilder output)
    {
      output.AppendLine($"  public static void AdjustAll(this {immutableType}<{r.FullyQualifiedRecordName}>.Builder b, System.Action<{r.FullyQualifiedInterfaceName}> f)");
      output.AppendLine("  {");
      output.AppendLine("    var orig = b.ToImmutable();");
      output.AppendLine("    b.Clear();");
      output.AppendLine("    b.AddRange(System.Linq.Enumerable.Select(orig, x => x.Produce(f)));");
      output.AppendLine("  }");

      if (includeIndexed)
      {
        output.AppendLine($"  public static void AdjustAll(this {immutableType}<{r.FullyQualifiedRecordName}>.Builder b, System.Action<{r.FullyQualifiedInterfaceName}, int> f)");
        output.AppendLine("  {");
        output.AppendLine("    var orig = b.ToImmutable();");
        output.AppendLine("    b.Clear();");
        output.AppendLine("    b.AddRange(System.Linq.Enumerable.Select(orig, (x, idx) => x.Produce(d => f(d, idx))));");
        output.AppendLine("  }");
      }
    }

    private static void EmitTwoTypeArg(string immutableType, DraftableRecord r, StringBuilder output)
    {
      output.AppendLine($"  public static void AdjustAll<Key>(this {immutableType}<Key, {r.FullyQualifiedRecordName}>.Builder b, System.Action<Key, {r.FullyQualifiedInterfaceName}> f) where Key : notnull");
      output.AppendLine("  {");
      output.AppendLine("    var orig = b.ToImmutable();");
      output.AppendLine("    b.Clear();");
      output.AppendLine("    b.AddRange(System.Linq.Enumerable.Select(orig, x => System.Collections.Generic.KeyValuePair.Create(x.Key, x.Value.Produce(d => f(x.Key, d)))));");
      output.AppendLine("  }");
    }
  }
}