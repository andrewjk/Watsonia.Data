using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
			// TODO: When a file changes, reload its tree and the parent-child mapping
			// TODO: Add a comment that tells you where the e.g. property name came from in the config
			// TODO: Do all config from attributes?? Or some sort of JSON?
			// TODO: Should the config only work on strings rather than properties?

			var configText = File.ReadAllText("./dataconfig.json");
			var options = new JsonSerializerOptions()
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
			var config = JsonSerializer.Deserialize<DataConfig>(configText, options);

			foreach (var rule in config.Rules)
			{
				// Load all files, and remove output files (so that we don't recursively create files)
				var files = LoadFiles(rule.Path, rule);
				var outputFolders = files.Select(f => f.OutputFolder).Distinct().ToList();
				files = files.Where(f => !outputFolders.Contains(f.InputFolder)).ToList();

				// Load all entities from the files
				var entities = LoadEntities(files);

				// Now that we know what entities are mapped, set related items and collections
				SetRelationships(entities);

				// Build a proxy for each entity
				foreach (var entity in entities)
				{
					// HACK: Need a better way to filter out enums etc
					if (string.IsNullOrEmpty(entity.Name))
					{
						continue;
					}

					Console.WriteLine("Writing " + entity.Name);

					if (!Directory.Exists(entity.OutputFolder))
					{
						Directory.CreateDirectory(entity.OutputFolder);
					}

					var proxy = Builder.CreateProxy(entity);
					var proxyFile = Path.Combine(entity.OutputFolder, entity.Name + "Proxy.cs");
					File.WriteAllText(proxyFile, proxy);

					var valueBag = Builder.CreateValueBag(entity);
					var valueBagFile = Path.Combine(entity.OutputFolder, entity.Name + "ValueBag.cs");
					File.WriteAllText(valueBagFile, valueBag);
				}
			}

			Console.WriteLine();
			Console.WriteLine("Press any key to exit...");
			Console.ReadLine();
		}

		static IList<MappedFile> LoadFiles(string path, DataConfigRule rule)
		{
			var regex = new Regex(rule.ShouldMapType[0].FileMatch.Replace("\\", "\\\\"));

			var files = new List<MappedFile>();
			foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
			{
				if (regex.IsMatch(file))
				{
					files.Add(new MappedFile()
					{
						InputFile = file,
						InputFolder = Path.GetDirectoryName(file).TrimEnd(Path.DirectorySeparatorChar),
						OutputFolder = regex.Replace(file, rule.ShouldMapType[0].Result).TrimEnd(Path.DirectorySeparatorChar)
					});
				}
			}
			Console.WriteLine();
			return files;
		}

		static IList<MappedEntity> LoadEntities(IList<MappedFile> files)
		{
			var entities = new List<MappedEntity>();
			foreach (var file in files)
			{
				Console.WriteLine("Loading " + file);
				var entity = Mapper.MapEntity(file.InputFile);
				entity.OutputFolder = file.OutputFolder;
				entities.Add(entity);
			}
			Console.WriteLine();
			return entities;
		}

		static void SetRelationships(IList<MappedEntity> entities)
		{
			Console.WriteLine("Setting relationships");

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
				var newProperties = new List<MappedProperty>();
				foreach (var prop in entity.Properties)
				{
					if (entities.Any(e => e.Name == prop.TypeName))
					{
						prop.IsRelatedItem = true;

						var itemIdName = prop.Name + "ID";
						if (!entity.Properties.Any(p => p.Name == itemIdName))
						{
							newProperties.Add(new MappedProperty()
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
							prop.CollectionTypeName = innerTypeName;

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

				entity.Properties.AddRange(newProperties);

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
