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
  public record RecordProperty
  {
    public PropertyDeclarationSyntax Decl { get; init; }
    public string PropertyName { get; init; }
    public INamedTypeSymbol PropertyType { get; init; }
    public string FullPropertyTypeName { get; init; }
    public IReadOnlyList<string> TypeArguments { get; init; }
  }


  public record RecordToDraft
  {
    public RecordDeclarationSyntax Decl { get; init; }
    public SemanticModel Model { get; init; }
    public string ClassName { get; init; }
    public string FullClassName { get; init; }
    public string DraftName { get; init; }
    public string InterfaceName { get; init; }
    public IReadOnlyList<RecordProperty> Properties { get; init; }
  }

  public static class BuildRecords
  {
    public static IReadOnlyDictionary<string, RecordToDraft> RecordsToDraft(Compilation comp, IEnumerable<RecordDeclarationSyntax> rdss)
    {
      return rdss.Select(rds =>
      {
        var model = comp.GetSemanticModel(rds.SyntaxTree);
        return new RecordToDraft()
        {
          Decl = rds,
          Model = model,
          ClassName = rds.Identifier.ToString(),
          FullClassName = model.GetDeclaredSymbol(rds).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
          DraftName = Names.DraftClassPrefix + rds.Identifier.ToString(),
          InterfaceName = "I" + rds.Identifier.ToString() + "Draft",
          Properties = rds.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p =>
            {
              var propType = model.GetSymbolInfo(p.Type).Symbol as INamedTypeSymbol;
              return new RecordProperty()
              {
                Decl = p,
                PropertyName = p.Identifier.ToString(),
                PropertyType = propType,
                FullPropertyTypeName = propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TypeArguments = propType.TypeArguments.Select(a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToList()
              };
            })
            .ToList(),
        };
      }).ToDictionary(r => r.FullClassName, r => r);
    }
  }
}
