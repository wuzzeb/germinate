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
  public static class PropNonDraftable
  {
    public static void Emit(EmitPhase phase, RecordProperty prop, StringBuilder output)
    {
      string typeName =
          (prop.IsValueType || prop.Nullable == Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated)
          ? prop.FullTypeName
          : prop.FullTypeName + "?";
      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {typeName} {prop.PropertyName} {{get; set;}}");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    protected {typeName} {Names.PropPrefix}{prop.PropertyName};");
          output.AppendLine($"    public {typeName} {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine($"      get => {Names.PropPrefix}{prop.PropertyName};");
          output.AppendLine("      set");
          output.AppendLine("      {");
          output.AppendLine($"        {Names.SetDirtyMethod}();");
          output.AppendLine($"        {Names.PropPrefix}{prop.PropertyName} = value;");
          output.AppendLine("      }");
          output.AppendLine("    }");
          break;

        case EmitPhase.Constructor:
          output.AppendLine($"      {Names.PropPrefix}{prop.PropertyName} = value.{prop.PropertyName};");
          break;

        case EmitPhase.Finish:
          output.AppendLine($"          {prop.PropertyName} = {Names.PropPrefix}{prop.PropertyName},");
          break;

      }
    }
  }
}