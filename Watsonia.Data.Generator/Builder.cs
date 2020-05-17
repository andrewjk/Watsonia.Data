using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Generator
{
	class Builder
	{
		static Dictionary<string, string> BuiltInTypeNames = new Dictionary<string, string>
		{
			{ "bool", "Boolean" },
			{ "bool?", "Boolean" },
			{ "byte", "Byte" },
			{ "byte?", "Byte" },
			{ "sbyte", "SByte" },
			{ "sbyte?", "SByte" },
			{ "char", "Char" },
			{ "char?", "Char" },
			{ "decimal", "Decimal" },
			{ "decimal?", "Decimal" },
			{ "double", "Double" },
			{ "double?", "Double" },
			{ "float", "Single" },
			{ "float?", "Single" },
			{ "int", "Int32" },
			{ "int?", "Int32" },
			{ "uint", "UInt32" },
			{ "uint?", "UInt32" },
			{ "long", "Int64" },
			{ "long?", "Int64" },
			{ "ulong", "UInt64" },
			{ "ulong?", "UInt64" },
			{ "short", "Int16" },
			{ "short?", "Int16" },
			{ "ushort", "UInt16" },
			{ "ushort?", "UInt16" },
			{ "string", "String" },
			{ "DateTime", "DateTime" },
			{ "DateTime?", "DateTime" },
			{ "object", "Object" },
		};

		public static string CreateProxy(MappedEntity entity)
		{
			var b = new StringBuilder();

			foreach (var u in entity.Usings)
			{
				b.AppendLine($"using {u};");
			}
			b.AppendLine();
			b.AppendLine("namespace Watsonia.Data.Generator.Proxies");
			b.AppendLine("{");
			b.AppendLine($"	public class {entity.Name}Proxy : {entity.Name}, IDynamicProxy");
			b.AppendLine("	{");

			CreatePrimaryKeyValueChangedEventHandler(b);
			b.AppendLine();

			CreateStateTrackerProperty(b);
			b.AppendLine();

			// Create each overridden property
			foreach (var prop in entity.Properties)
			{
				CreateOverriddenProperty(b, prop);
			}
			b.AppendLine();

			// Create the primary key property (e.g. ID or TableID) if it didn't already get created
			if (!entity.Properties.Any(p => p.Name == "ID"))
			{
				CreateFieldProperty(b, new MappedProperty()
				{
					Name = "ID",
					TypeName = "long"
				});
			}

			// Create the __PrimaryKeyValue property which just wraps the primary key property
			CreatePrimaryKeyValueProperty(b);

			// Create the related item properties
			foreach (var prop in entity.Properties.Where(p => p.IsRelatedItem))
			{
				CreateOverriddenItemProperty(b, prop);
			}

			// TODO: Create the related item properties for parent-child relationships that don't exist as properties
			//if (childParentMapping.ContainsKey(parentType))
			//{
			//	foreach (var parent in childParentMapping[parentType])
			//	{
			//		// If the ID property doesn't exist, create it
			//		var propertyName = database.Configuration.GetForeignKeyColumnName(parentType, parent);
			//		if (!members.GetPropertyMethods.ContainsKey(propertyName))
			//		{
			//			var propertyType = database.Configuration.GetPrimaryKeyColumnType(parent);
			//			if (propertyType.IsValueType)
			//			{
			//				propertyType = typeof(Nullable<>).MakeGenericType(propertyType);
			//			}
			//			CreateFieldProperty(type, propertyName, propertyType, members);
			//		}
			//	}
			//}

			CreateConstructor(b, entity);
			b.AppendLine();

			CreateGetValueMethod(b, entity);
			b.AppendLine();

			CreateSetValueMethod(b, entity);
			b.AppendLine();

			CreateSetOriginalValuesMethod(b, entity);
			b.AppendLine();

			CreateSetValuesFromReaderMethod(b, entity);
			b.AppendLine();

			CreateGetBagFromValuesMethod(b, entity);
			b.AppendLine();

			CreateSetValuesFromBagMethod(b, entity);
			b.AppendLine();

			CreateEqualityMethods(b, entity);

			b.AppendLine("	}");
			b.AppendLine("}");

			return b.ToString();
		}

		private static void CreatePrimaryKeyValueChangedEventHandler(StringBuilder b)
		{
			b.AppendLine("		public event PrimaryKeyValueChangedEventHandler __PrimaryKeyValueChanged;");
			b.AppendLine("		private void OnPrimaryKeyValueChanged(object value)");
			b.AppendLine("		{");
			b.AppendLine("			__PrimaryKeyValueChanged?.Invoke(this, new PrimaryKeyValueChangedEventArgs(value));");
			b.AppendLine("		}");
		}

		private static void CreateStateTrackerProperty(StringBuilder b)
		{
			b.AppendLine("		private DynamicProxyStateTracker _stateTracker;");
			b.AppendLine("		public DynamicProxyStateTracker StateTracker");
			b.AppendLine("		{");
			b.AppendLine("			get");
			b.AppendLine("			{");
			b.AppendLine("				if (_stateTracker == null)");
			b.AppendLine("				{");
			b.AppendLine("					_stateTracker = new DynamicProxyStateTracker();");
			b.AppendLine("					_stateTracker.Item = this;");
			b.AppendLine("				}");
			b.AppendLine("				return _stateTracker;");
			b.AppendLine("			}");
			b.AppendLine("		}");
		}

		private static void CreateOverriddenProperty(StringBuilder b, MappedProperty prop)
		{
			b.AppendLine($"		public override {prop.TypeName} {prop.Name}");
			b.AppendLine("		{");
			b.AppendLine("			get");
			b.AppendLine("			{");
			b.AppendLine($"				return base.{prop.Name};");
			b.AppendLine("			}");
			b.AppendLine("			set");
			b.AppendLine("			{");
			b.AppendLine($"				base.{prop.Name} = value;");
			b.AppendLine($"				this.StateTracker.SetFieldValue(nameof({prop.Name}), value);");
			if (prop.Name == "ID")
			{
				b.AppendLine($"				OnPrimaryKeyValueChanged(value);");
			}
			b.AppendLine("			}");
			b.AppendLine("		}");
			b.AppendLine();
		}

		private static void CreateFieldProperty(StringBuilder b, MappedProperty prop)
		{
			b.AppendLine($"		private {prop.TypeName} _{prop.Name};");
			b.AppendLine($"		public {prop.TypeName} {prop.Name}");
			b.AppendLine("		{");
			b.AppendLine("			get");
			b.AppendLine("			{");
			b.AppendLine($"				return _{prop.Name};");
			b.AppendLine("			}");
			b.AppendLine("			set");
			b.AppendLine("			{");
			b.AppendLine($"				_{prop.Name} = value;");
			b.AppendLine($"				this.StateTracker.SetFieldValue(nameof({prop.Name}), value);");
			if (prop.Name == "ID")
			{
				b.AppendLine($"				OnPrimaryKeyValueChanged(value);");
			}
			b.AppendLine("			}");
			b.AppendLine("		}");
			b.AppendLine();
		}

		private static void CreatePrimaryKeyValueProperty(StringBuilder b)
		{
			b.AppendLine("		public object __PrimaryKeyValue");
			b.AppendLine("		{");
			b.AppendLine("			get");
			b.AppendLine("			{");
			b.AppendLine("				return this.ID;");
			b.AppendLine("			}");
			b.AppendLine("			set");
			b.AppendLine("			{");
			b.AppendLine("				this.ID = (long)Convert.ChangeType(value, typeof(long));");
			b.AppendLine("			}");
			b.AppendLine("		}");
			b.AppendLine();
		}

		private static void CreateOverriddenItemProperty(StringBuilder b, MappedProperty prop)
		{
			throw new NotImplementedException();
		}

		private static void CreateConstructor(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine($"		public {entity.Name}Proxy()");
			b.AppendLine("		{");
			b.AppendLine("			this.StateTracker.IsLoading = true;");
			b.AppendLine();
			foreach (var prop in entity.Properties)
			{
				var defaultValue = "";
				foreach (var att in prop.Attributes)
				{
					if (att.Name == "DefaultValue" ||
						att.Name == "DefaultValueAttribute")
					{
						defaultValue = $"{string.Join(", ", att.Arguments)}";
					}
					else if (
						att.Name == "DefaultDateTimeValue" ||
						att.Name == "DefaultDateTimeValueAttribute")
					{
						defaultValue = $"new DateTime({string.Join(", ", att.Arguments)})";
					}
				}

				if (!string.IsNullOrEmpty(defaultValue))
				{
					b.AppendLine($"			this.{prop.Name} = {defaultValue};");
				}
				else if (prop.TypeName == "string")
				{
					b.AppendLine($"			if (this.{prop.Name} == null)");
					b.AppendLine("			{");
					b.AppendLine($"				this.{prop.Name} = \"\";");
					b.AppendLine("			}");
				}
			}
			b.AppendLine("");
			b.AppendLine("			this.__SetOriginalValues();");
			b.AppendLine("");
			b.AppendLine("			this.StateTracker.IsLoading = false;");
			b.AppendLine("		}");
		}

		private static void CreateGetValueMethod(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public object __GetValue(string name)");
			b.AppendLine("		{");
			b.AppendLine("			switch (name.ToUpperInvariant())");
			b.AppendLine("			{");
			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"				case \"{prop.Name.ToUpperInvariant()}\":");
				b.AppendLine("				{");
				b.AppendLine($"					return this.{prop.Name};");
				b.AppendLine("				}");
			}
			b.AppendLine("			}");
			b.AppendLine("");
			b.AppendLine("			throw new ArgumentException(name);");
			b.AppendLine("		}");
		}

		private static void CreateSetValueMethod(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public void __SetValue(string name, object value)");
			b.AppendLine("		{");
			b.AppendLine("			switch (name.ToUpperInvariant())");
			b.AppendLine("			{");
			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"				case \"{prop.Name.ToUpperInvariant()}\":");
				b.AppendLine("				{");
				b.AppendLine($"					this.{prop.Name} = ({prop.TypeName})value;");
				b.AppendLine("					break;");
				b.AppendLine("				}");
			}
			b.AppendLine("			}");
			b.AppendLine("");
			b.AppendLine("			throw new ArgumentException(name);");
			b.AppendLine("		}");
		}

		private static void CreateSetOriginalValuesMethod(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public void __SetOriginalValues()");
			b.AppendLine("		{");
			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"			this.StateTracker.OriginalValues[\"{prop.Name}\"] = this.{prop.Name};");
			}
			b.AppendLine("		}");
		}

		private static void CreateSetValuesFromReaderMethod(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public void __SetValuesFromReader(DbDataReader source, string[] fieldNames)");
			b.AppendLine("		{");
			b.AppendLine("			this.StateTracker.IsLoading = true;");
			b.AppendLine("");
			b.AppendLine("			for (var i = 0; i < fieldNames.Length; i++)");
			b.AppendLine("			{");
			b.AppendLine("				switch (fieldNames[i])");
			b.AppendLine("				{");
			foreach (var prop in entity.Properties)
			{
				// TODO: set the IsRelated properties
				if (!BuiltInTypeNames.ContainsKey(prop.TypeName))
				{
					continue;
				}

				b.AppendLine($"					case \"{prop.Name.ToUpperInvariant()}\":");
				b.AppendLine("					{");
				b.AppendLine($"						this.{prop.Name} = source.Get{BuiltInTypeNames[prop.TypeName]}(i);");
				b.AppendLine("						break;");
				b.AppendLine("					}");
			}
			b.AppendLine("				}");
			b.AppendLine("			}");
			b.AppendLine("");
			b.AppendLine("			this.__SetOriginalValues();");
			b.AppendLine("");
			b.AppendLine("			this.StateTracker.IsLoading = false;");
			b.AppendLine("		}");
		}

		private static void CreateGetBagFromValuesMethod(StringBuilder b, MappedEntity entity)
		{

			b.AppendLine("		public IValueBag __GetBagFromValues()");
			b.AppendLine("		{");
			b.AppendLine($"			var {entity.Name.ToLowerInvariant()}Bag = new {entity.Name}ValueBag();");
			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"			{entity.Name.ToLowerInvariant()}Bag.{prop.Name} = this.{prop.Name};");
			}
			b.AppendLine($"			return {entity.Name.ToLowerInvariant()}Bag;");
			b.AppendLine("		}");
		}

		private static void CreateSetValuesFromBagMethod(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public void __SetValuesFromBag(IValueBag bag)");
			b.AppendLine("		{");
			b.AppendLine("			this.StateTracker.IsLoading = true;");
			b.AppendLine();
			b.AppendLine($"			var {entity.Name.ToLowerInvariant()}Bag = ({entity.Name}ValueBag)bag;");
			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"			this.{prop.Name} = {entity.Name.ToLowerInvariant()}Bag.{prop.Name};");
			}
			b.AppendLine();
			b.AppendLine("			this.__SetOriginalValues();");
			b.AppendLine();
			b.AppendLine("			this.StateTracker.IsLoading = false;");
			b.AppendLine("		}");
		}

		private static void CreateEqualityMethods(StringBuilder b, MappedEntity entity)
		{
			b.AppendLine("		public override int GetHashCode()");
			b.AppendLine("		{");
			b.AppendLine("			return this.StateTracker.GetItemHashCode();");
			b.AppendLine("		}");
			b.AppendLine("");
			b.AppendLine("		public override bool Equals(object obj)");
			b.AppendLine("		{");
			b.AppendLine("			return this.StateTracker.ItemEquals(obj);");
			b.AppendLine("		}");
			b.AppendLine("");
			b.AppendLine($"		public static bool operator ==({entity.Name}Proxy a, {entity.Name}Proxy b)");
			b.AppendLine("		{");
			b.AppendLine("			if (object.ReferenceEquals(a, b))");
			b.AppendLine("			{");
			b.AppendLine("				return true;");
			b.AppendLine("			}");
			b.AppendLine("");
			b.AppendLine("			if ((a is null) || (b is null))");
			b.AppendLine("			{");
			b.AppendLine("				return false;");
			b.AppendLine("			}");
			b.AppendLine("");
			b.AppendLine("			return a.Equals(b);");
			b.AppendLine("		}");
			b.AppendLine("");
			b.AppendLine($"		public static bool operator !=({entity.Name}Proxy a, {entity.Name}Proxy b)");
			b.AppendLine("		{");
			b.AppendLine("			return !(a == b);");
			b.AppendLine("		}");
		}

		public static string CreateValueBag(MappedEntity entity)
		{
			var b = new StringBuilder();

			foreach (var u in entity.Usings)
			{
				b.AppendLine($"using {u};");
			}
			b.AppendLine();
			b.AppendLine("namespace Watsonia.Data.Generator.Proxies");
			b.AppendLine("{");
			b.AppendLine($"	public class {entity.Name}ValueBag : IValueBag");
			b.AppendLine("	{");

			foreach (var prop in entity.Properties)
			{
				b.AppendLine($"		public {prop.TypeName} {prop.Name} {{ get; set; }}");
			}

			b.AppendLine("	}");
			b.AppendLine("}");

			return b.ToString();
		}
	}
}
