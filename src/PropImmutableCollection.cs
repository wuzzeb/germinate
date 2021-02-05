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
  public static class PropImmutableCollection
  {
    public static void Emit(EmitPhase phase, RecordProperty prop, StringBuilder output)
    {
      var imProp = Names.PropPrefix + "o_" + prop.PropertyName;
      var builderProp = Names.PropPrefix + "b_" + prop.PropertyName;

      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {prop.FullPropertyTypeName}.Builder {prop.PropertyName} {{get;}}");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    private {prop.FullPropertyTypeName} {imProp};");
          output.AppendLine($"    private {prop.FullPropertyTypeName}.Builder {builderProp} = null;");
          output.AppendLine($"    public {prop.FullPropertyTypeName}.Builder {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine("      get");
          output.AppendLine("      {");
          output.AppendLine($"        if ({builderProp} == null) {{");
          output.AppendLine($"          {builderProp} = ({imProp} ?? {prop.FullPropertyTypeName}.Empty).ToBuilder();");
          output.AppendLine($"          base.{Names.AddCheckDirtyMethod}(() => {{");
          output.AppendLine($"            var newVal = {builderProp}.ToImmutable();");
          output.AppendLine($"            if (!object.ReferenceEquals(newVal, {imProp})) {{");
          output.AppendLine($"              base.{Names.SetDirtyMethod}();");
          output.AppendLine($"              {imProp} = newVal;");
          output.AppendLine("            }");
          output.AppendLine($"            {builderProp} = null;");
          output.AppendLine("          });");
          output.AppendLine("        }");
          output.AppendLine($"        return {builderProp};");
          output.AppendLine("      }");
          output.AppendLine("    }");
          break;

        case EmitPhase.Constructor:
          output.AppendLine($"      {imProp} = value.{prop.PropertyName};");
          break;

        case EmitPhase.Finish:
          // The CheckDirty method above is called before Finish, and sets any changes back into imProp
          output.AppendLine($"          {prop.PropertyName} = this.{imProp},");
          break;

      }
    }
  }
}
