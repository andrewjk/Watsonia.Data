using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;

namespace Watsonia.Data
{
	/// <summary>
	/// The interface for a dynamic proxy created from an entity.
	/// </summary>
	public interface IDynamicProxy
	{
		/// <summary>
		/// Occurs when the value of the primary key value has changed.
		/// </summary>
		event PrimaryKeyValueChangedEventHandler __PrimaryKeyValueChanged;

		/// <summary>
		/// Gets the state tracker.
		/// </summary>
		/// <value>
		/// The state tracker.
		/// </value>
		DynamicProxyStateTracker StateTracker
		{
			get;
		}

		/// <summary>
		/// Gets or sets the primary key value of this instance in the database.
		/// </summary>
		/// <value>
		/// The primary key value.
		/// </value>
		object __PrimaryKeyValue { get; set; }

		/// <summary>
		/// Sets the original values after loading or saving.
		/// </summary>
		void __SetOriginalValues();

		/// <summary>
		/// Sets the item's property values from a data reader.
		/// </summary>
		/// <param name="source">The data reader.</param>
		void __SetValuesFromReader(DbDataReader source);

		/// <summary>
		/// Sets the item's property values from a value bag.
		/// </summary>
		/// <param name="bag">The value bag.</param>
		void __SetValuesFromBag(IValueBag bag);

		/// <summary>
		/// Gets a value bag from the item's property values.
		/// </summary>
		/// <returns></returns>
		IValueBag __GetBagFromValues();
	}
}
