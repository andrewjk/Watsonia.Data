using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// The type of item that will be loaded in a collection.
	/// </summary>
	internal enum CollectionItemType
	{
		/// <summary>
		/// The item is a value e.g. an int or a string.
		/// </summary>
		Value,
		/// <summary>
		/// The item is an anonymous type e.g. from a LINQ query.
		/// </summary>
		Anonymous,
		/// <summary>
		/// The item is a DynamicProxy.
		/// </summary>
		DynamicProxy,
		/// <summary>
		/// The item is an object that should be converted into a DynamicProxy.
		/// </summary>
		MappedObject,
		/// <summary>
		/// The item is a plain old object that shouldn't be converted into a DynamicProxy.
		/// </summary>
		PlainObject,
	}
}
