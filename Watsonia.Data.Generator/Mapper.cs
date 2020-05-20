using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Watsonia.Data.Generator
{
	static class Mapper
	{
		internal static MappedEntity MapEntity(string file)
		{
			var entity = new MappedEntity();
			entity.FileName = file;

			var content = File.ReadAllText(file);
			var tree = CSharpSyntaxTree.ParseText(content);

			//// TODO: Can we get better type info out of here??
			//var root = (CompilationUnitSyntax)tree.GetRoot();
			//var compilation = CSharpCompilation.Create("HelloWorld")
			//				  .AddReferences(
			//					 MetadataReference.CreateFromFile(
			//					   typeof(object).Assembly.Location))
			//				  .AddSyntaxTrees(tree);
			//var model = compilation.GetSemanticModel(tree);

			foreach (var node in tree.GetRoot().DescendantNodes())
			{
				switch (node.Kind())
				{
					case SyntaxKind.UsingDirective:
					{
						var use = (UsingDirectiveSyntax)node;
						entity.Usings.Add(GetName(use.Name));
						break;
					}
					case SyntaxKind.NamespaceDeclaration:
					{
						var ns = (NamespaceDeclarationSyntax)node;
						entity.Namespace = GetName(ns.Name);
						break;
					}
					case SyntaxKind.ClassDeclaration:
					{
						var cls = (ClassDeclarationSyntax)node;
						entity.Name = cls.Identifier.ValueText;
						break;
					}
					case SyntaxKind.PropertyDeclaration:
					{
						var prop = (PropertyDeclarationSyntax)node;
						if (prop.Modifiers.Any(m => m.ValueText == "virtual"))
						{
							//// TODO: Probably need to be smarter about types??
							//if (prop.Type is PredefinedTypeSyntax pt)
							//{
							//	var type = pt.Keyword.ValueText;
							//}

							//var type = model.GetDeclaredSymbol(prop);

							var property = new MappedProperty()
							{
								Name = prop.Identifier.ValueText,
								TypeName = prop.Type.GetText().ToString().Trim(),
								IsOverridden = true
							};

							if (prop.AttributeLists.Any())
							{
								var att = prop.AttributeLists[0].Attributes[0];
								var attName = GetName(att.Name);
								property.Attributes.Add(new MappedAttribute()
								{
									Name = attName,
									Arguments = att.ArgumentList?.Arguments.Select(a => a.ToString()).ToList()
								});
							}

							entity.Properties.Add(property);
						}
						break;
					}
				}
			}

			if (!entity.Usings.Contains("System")) entity.Usings.Add("System");
			if (!entity.Usings.Contains("System.Data.Common")) entity.Usings.Add("System.Data.Common");
			if (!entity.Usings.Contains("Watsonia.Data")) entity.Usings.Add("Watsonia.Data");
			if (!entity.Usings.Contains("Watsonia.Data.EventArgs")) entity.Usings.Add("Watsonia.Data.EventArgs");

			// TODO: Better sort
			entity.Usings.Sort();

			return entity;
		}

		private static string GetName(NameSyntax name)
		{
			if (name is IdentifierNameSyntax iu)
			{
				return iu.Identifier.ValueText;
			}
			else if (name is QualifiedNameSyntax qu)
			{
				return qu.GetText().ToString();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
	}
}
