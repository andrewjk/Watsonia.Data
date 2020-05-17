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
	class Program
	{
		static void Main(string[] args)
		{
			// Load all entities into trees
			// Convert all trees into MappedEntities
			// TODO: Build a parent-child mapping (or schema info)
			// TODO: When a file changes, reload that tree and the parent-child mapping
			// TODO: Add a comment that tells you where the e.g. property name came from in the config
			// TODO: Do all config from attributes?? Or some sort of JSON?

			var folder = @"..\..\..\Entities";
			var entities = new List<MappedEntity>();
			foreach (var file in Directory.EnumerateFiles(folder))
			{
				var entity = Mapper.MapEntity(file);
				entities.Add(entity);
			}

			foreach (var entity in entities)
			{
				// TODO: Only do this if the file is newer
				var proxy = Builder.CreateProxy(entity);
				var valueBag = Builder.CreateValueBag(entity);

				File.WriteAllText(@$"..\..\..\Entities\Proxies\{entity.Name}Proxy.cs", proxy);
				File.WriteAllText(@$"..\..\..\Entities\Proxies\{entity.Name}ValueBag.cs", valueBag);
			}
		}
	}
}
