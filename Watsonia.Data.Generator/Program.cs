using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Watsonia.Data.Generator
{
	class Program
	{
		static void Main(string[] args)
		{
			// NOTE: This would be a good candidate for a Source Generator
			// https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/

			// TODO: Build a parent-child mapping (or schema info)
			// TODO: When a file changes, reload that tree and the parent-child mapping
			// TODO: Add a comment that tells you where the e.g. property name came from in the config
			// TODO: Do all config from attributes?? Or some sort of JSON?

			// TODO: Add IsRelatedItem, IsRelatedCollection, ShouldCascade, etc methods to each proxy

			// Load all entities into MappedEntities
			var folder = @"..\..\..\Entities";
			var entities = LoadEntities(folder);

			// Now that we know what entities are mapped, set related items and collections
			SetRelationships(entities);

			SetRelationships(entities);

			// Build a proxy for each entity
			foreach (var entity in entities)
			{
				Console.WriteLine("Writing " + entity.Name);

				var proxy = Builder.CreateProxy(entity);
				File.WriteAllText(@$"..\..\..\Entities\Proxies\{entity.Name}Proxy.cs", proxy);

				var valueBag = Builder.CreateValueBag(entity);
				File.WriteAllText(@$"..\..\..\Entities\Proxies\{entity.Name}ValueBag.cs", valueBag);
			}

			Console.WriteLine();
			Console.WriteLine("Press any key to exit...");
			Console.ReadLine();
		}

		static IList<MappedEntity> LoadEntities(string folder)
		{
			var entities = new List<MappedEntity>();
			foreach (var file in Directory.EnumerateFiles(folder))
			{
				Console.WriteLine("Loading " + file);
				var entity = Mapper.MapEntity(file);
				entities.Add(entity);
			}
			Console.WriteLine();
			return entities;
		}

		static void SetRelationships(IList<MappedEntity> entities)
		{
			Console.WriteLine("Setting Relationships");

			// Remove all generated relationship properties
			foreach (var entity in entities)
			{
				for (var i = entity.Properties.Count - 1; i >= 0; i--)
				{
					if (entity.Properties[i].IsGenerated)
					{
						entity.Properties.RemoveAt(i);
					}
				}
			}

			// HACK: Need much more robust configuration here
			var collectionRegex = new Regex("(?:List|Collection)<(.+?)>");
			foreach (var entity in entities)
			{
				foreach (var prop in entity.Properties)
				{
					if (entities.Any(e => e.Name == prop.TypeName))
					{
						prop.IsRelatedItem = true;

						var itemIdName = prop.Name + "ID";
						if (!entity.Properties.Any(p => p.Name == itemIdName))
						{
							entity.Properties.Add(new MappedProperty()
							{
								Name = itemIdName,
								TypeName = "long?",
								IsGenerated = true
							});
						}
					}
					else if (collectionRegex.IsMatch(prop.TypeName))
					{
						var innerTypeName = collectionRegex.Match(prop.TypeName).Groups[1].Value;
						if (entities.Any(e => e.Name == innerTypeName))
						{
							prop.IsRelatedCollection = true;
							prop.InnerTypeName = innerTypeName;

							var parentIdName = entity.Name + "ID";
							var child = entities.First(e => e.Name == innerTypeName);
							if (!child.Properties.Any(p => p.Name == parentIdName))
							{
								child.Properties.Add(new MappedProperty()
								{
									Name = parentIdName,
									TypeName = "long?",
									IsGenerated = true
								});
							}
						}
					}
				}

				if (!entity.Properties.Any(p => p.Name == "ID"))
				{
					entity.Properties.Insert(0, new MappedProperty()
					{
						Name = "ID",
						TypeName = "long",
						IsGenerated = true
					});
				}
			}

			Console.WriteLine();
		}
	}
}
