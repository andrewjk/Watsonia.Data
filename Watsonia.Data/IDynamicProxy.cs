using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;

namespace Watsonia.Data
{
	/// <summary>
	/// The interface for a dynamic proxy created from an entity.
	/// </summary>
	public interface IDynamicProxy : INotifyPropertyChanging, INotifyPropertyChanged, IDataErrorInfo
	{
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
		object PrimaryKeyValue
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is new (i.e. doesn't exist in the database).
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is new; otherwise, <c>false</c>.
		/// </value>
		bool IsNew
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance has changes.
		/// </summary>
		/// <value>
		///	  <c>true</c> if this instance has changes; otherwise, <c>false</c>.
		/// </value>
		bool HasChanges
		{
			get;
			set;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is in a valid state.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
		/// </value>
		bool IsValid
		{
			get;
		}

		/// <summary>
		/// Gets the validation errors that apply to this instance.
		/// </summary>
		/// <value>
		/// The validation errors.
		/// </value>
		IList<ValidationError> ValidationErrors
		{
			get;
		}

		/// <summary>
		/// Sets the item's property values from a data reader.
		/// </summary>
		/// <param name="source">The data reader.</param>
		void SetValuesFromReader(DbDataReader source);
	}
}
