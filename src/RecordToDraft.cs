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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Germinate.Generator
{
  // These should be records but source generators need to target netstandard2.0
  public class RecordProperty
  {
    public string PropertyName { get; set; }
    public string FullTypeName { get; set; }
    public NullableAnnotation Nullable { get; set; }
    public bool IsValueType { get; set; }
  }


  public class RecordToDraft
  {
    public string ClassName { get; set; }
    public string FullClassName { get; set; }
    public string DraftName { get; set; }
    public string InterfaceName { get; set; }
    public IReadOnlyList<RecordProperty> Properties { get; set; }
  }

  public static class BuildRecords
  {
    public static IReadOnlyDictionary<string, RecordToDraft> RecordsToDraft(Compilation comp, IEnumerable<RecordDeclarationSyntax> rdss)
    {
      return rdss.Select((rds, idx) =>
      {
        var model = comp.GetSemanticModel(rds.SyntaxTree);
        return new RecordToDraft()
        {
          ClassName = rds.Identifier.ToString(),
          FullClassName = model.GetDeclaredSymbol(rds).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
          DraftName = Names.DraftClassPrefix + idx.ToString() + "_" + rds.Identifier.ToString(),
          InterfaceName = "I" + rds.Identifier.ToString() + "Draft",
          Properties = rds.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p =>
            {
              var propSymbol = model.GetDeclaredSymbol(p) as IPropertySymbol;
              return new RecordProperty()
              {
                PropertyName = p.Identifier.ToString(),
                Nullable = propSymbol.NullableAnnotation,
                FullTypeName = propSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsValueType = propSymbol.Type.IsValueType,
              };
            })
            .ToList(),
        };
      }).ToDictionary(r => r.FullClassName, r => r);
    }
  }
}
