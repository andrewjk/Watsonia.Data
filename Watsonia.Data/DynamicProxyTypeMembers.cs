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
		private readonly List<PropertyInfo> _baseItemProperties = new List<PropertyInfo>();
		private readonly Dictionary<string, MethodBuilder> _getPropertyMethods = new Dictionary<string, MethodBuilder>();
		private readonly Dictionary<string, MethodBuilder> _setPropertyMethods = new Dictionary<string, MethodBuilder>();
		private readonly Dictionary<string, MethodBuilder> _getRelatedItemIDMethods = new Dictionary<string, MethodBuilder>();
		private readonly Dictionary<string, MethodBuilder> _setRelatedItemIDMethods = new Dictionary<string, MethodBuilder>();
		private readonly Dictionary<string, object> _defaultValues = new Dictionary<string, object>();

		public string PrimaryKeyColumnName
		{
			get;
			set;
		}

		public Type PrimaryKeyColumnType
		{
			get;
			set;
		}

		public MethodBuilder GetStateTrackerMethod
		{
			get;
			set;
		}

		public MethodBuilder GetPrimaryKeyMethod
		{
			get;
			set;
		}

		public MethodBuilder SetPrimaryKeyMethod
		{
			get;
			set;
		}

		public List<PropertyInfo> BaseItemProperties
		{
			get
			{
				return _baseItemProperties;
			}
		}

		public Dictionary<string, MethodBuilder> GetPropertyMethods
		{
			get
			{
				return _getPropertyMethods;
			}
		}

		public Dictionary<string, MethodBuilder> SetPropertyMethods
		{
			get
			{
				return _setPropertyMethods;
			}
		}

		public Dictionary<string, object> DefaultValues
		{
			get
			{
				return _defaultValues;
			}
		}

		public MethodBuilder OnPropertyChangingMethod
		{
			get;
			set;
		}

		public MethodBuilder OnPropertyChangedMethod
		{
			get;
			set;
		}
	}
}
