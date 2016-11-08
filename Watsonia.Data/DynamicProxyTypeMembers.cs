using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Watsonia.Data
{
	/// <summary>
	/// A small class to keep track of properties and methods that we've added to a dynamic proxy.
	/// </summary>
	internal sealed class DynamicProxyTypeMembers
	{
		public string PrimaryKeyColumnName { get; set; }

		public Type PrimaryKeyColumnType { get; set; }

		public MethodBuilder GetStateTrackerMethod { get; set; }

		public MethodBuilder GetPrimaryKeyMethod { get; set; }

		public MethodBuilder SetPrimaryKeyMethod { get; set; }

		public List<PropertyInfo> BaseItemProperties { get; } = new List<PropertyInfo>();

		public Dictionary<string, MethodBuilder> GetPropertyMethods { get; } = new Dictionary<string, MethodBuilder>();

		public Dictionary<string, MethodBuilder> SetPropertyMethods { get; } = new Dictionary<string, MethodBuilder>();

		public List<string> ValueBagPropertyNames { get; } = new List<string>();

		public Type ValueBagType { get; set; }

		public Dictionary<string, MethodBuilder> GetValueBagPropertyMethods { get; } = new Dictionary<string, MethodBuilder>();

		public Dictionary<string, MethodBuilder> SetValueBagPropertyMethods { get; } = new Dictionary<string, MethodBuilder>();

		public Dictionary<string, object> DefaultValues { get; } = new Dictionary<string, object>();

		public MethodBuilder ResetOriginalValuesMethod { get; set; }

		public MethodBuilder OnPropertyChangingMethod { get; set; }

		public MethodBuilder OnPropertyChangedMethod { get; set; }
	}
}
