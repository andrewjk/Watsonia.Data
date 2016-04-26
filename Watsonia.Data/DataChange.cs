using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	public class DataChange : IComparable<DataChange>
	{
		/// <summary>
		/// Gets the date and time that this change was made.
		/// </summary>
		/// <value>
		/// The date time.
		/// </value>
		public DateTime DateTime
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the type of change.
		/// </summary>
		/// <value>
		/// The type of change.
		/// </value>
		public ChangeType ChangeType
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the proxy.
		/// </summary>
		/// <value>
		/// The proxy.
		/// </value>
		public IDynamicProxy Proxy
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		/// <value>
		/// The name of the property.
		/// </value>
		public string PropertyName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the old value.
		/// </summary>
		/// <value>
		/// The old value.
		/// </value>
		public object OldValue
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the new value.
		/// </summary>
		/// <value>
		/// The new value.
		/// </value>
		public object NewValue
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating the value of the proxy's HasChanges property before this change.
		/// </summary>
		/// <value>
		/// <c>true</c> if the proxy's HasChanges is true; otherwise, <c>false</c>.
		/// </value>
		public bool HasChanges
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataChange" /> class.
		/// </summary>
		/// <param name="changeType">The type of change.</param>
		/// <param name="proxy">The proxy.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="hasChanges">if set to <c>true</c> [has changes].</param>
		public DataChange(ChangeType changeType, IDynamicProxy proxy, string propertyName, object oldValue, object newValue, bool hasChanges)
		{
			this.DateTime = DateTime.Now;
			this.ChangeType = changeType;
			this.Proxy = proxy;
			this.PropertyName = propertyName;
			this.OldValue = oldValue;
			this.NewValue = newValue;
			this.HasChanges = hasChanges;
		}

		public void Undo()
		{
			this.Proxy.StateTracker.IsLoading = true;
			switch (this.ChangeType)
			{
				case ChangeType.PropertyValue:
				{
					this.Proxy.GetType().GetProperty(this.PropertyName).SetValue(this.Proxy, this.OldValue);
					this.Proxy.HasChanges = this.HasChanges;
					break;
				}
				case ChangeType.CollectionAdd:
				{
					IList list = (IList)this.Proxy.GetType().GetProperty(this.PropertyName).GetValue(this.Proxy);
					list.Remove(this.NewValue);
					break;
				}
				case ChangeType.CollectionRemove:
				{
					// TODO: Put it back in the same spot!
					IList list = (IList)this.Proxy.GetType().GetProperty(this.PropertyName).GetValue(this.Proxy);
					list.Add(this.OldValue);
					break;
				}
				default:
				{
					throw new InvalidOperationException("Invalid change type: " + this.ChangeType);
				}
			}
			this.Proxy.StateTracker.IsLoading = false;
		}

		public void Redo()
		{
			this.Proxy.StateTracker.IsLoading = true;
			switch (this.ChangeType)
			{
				case ChangeType.PropertyValue:
				{
					this.Proxy.GetType().GetProperty(this.PropertyName).SetValue(this.Proxy, this.NewValue);
					this.Proxy.HasChanges = true;
					break;
				}
				case ChangeType.CollectionAdd:
				{
					// TODO: Put it back in the same spot!
					IList list = (IList)this.Proxy.GetType().GetProperty(this.PropertyName).GetValue(this.Proxy);
					list.Add(this.NewValue);
					break;
				}
				case ChangeType.CollectionRemove:
				{
					IList list = (IList)this.Proxy.GetType().GetProperty(this.PropertyName).GetValue(this.Proxy);
					list.Remove(this.OldValue);
					break;
				}
				default:
				{
					throw new InvalidOperationException("Invalid change type: " + this.ChangeType);
				}
			}
			this.Proxy.StateTracker.IsLoading = false;
		}

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This object is less than the <paramref name="other" /> parameter.
		/// Zero
		/// This object is equal to <paramref name="other" />.
		/// Greater than zero
		/// This object is greater than <paramref name="other" />.
		/// </returns>
		public int CompareTo(DataChange other)
		{
			return this.DateTime.CompareTo(other.DateTime) * -1;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">Invalid change type:  + this.ChangeType</exception>
		public override string ToString()
		{
			switch (this.ChangeType)
			{
				case ChangeType.PropertyValue:
				{
					return string.Format("{0} Changed: {1} => {2}", this.PropertyName, this.OldValue, this.NewValue);
				}
				case ChangeType.CollectionAdd:
				{
					return string.Format("{0} Added: {1}", this.PropertyName, this.NewValue);
				}
				case ChangeType.CollectionRemove:
				{
					return string.Format("{0} Removed: {1}", this.PropertyName, this.OldValue);
				}
				default:
				{
					throw new InvalidOperationException("Invalid change type: " + this.ChangeType);
				}
			}			
		}
	}

	//public sealed class DataChange<T> : DataChange
	//{
	//	/// <summary>
	//	/// Gets the old value.
	//	/// </summary>
	//	/// <value>
	//	/// The old value.
	//	/// </value>
	//	public T OldValue
	//	{
	//		get;
	//		private set;
	//	}

	//	/// <summary>
	//	/// Gets the new value.
	//	/// </summary>
	//	/// <value>
	//	/// The new value.
	//	/// </value>
	//	public T NewValue
	//	{
	//		get;
	//		private set;
	//	}

	//	/// <summary>
	//	/// Initializes a new instance of the <see cref="DataChange{T}" /> class.
	//	/// </summary>
	//	/// <param name="changeType">The type of change.</param>
	//	/// <param name="proxy">The proxy.</param>
	//	/// <param name="property">The property.</param>
	//	/// <param name="oldValue">The old value.</param>
	//	/// <param name="newValue">The new value.</param>
	//	public DataChange(ChangeType changeType, IDynamicProxy proxy, PropertyInfo property, T oldValue, T newValue)
	//		: base(changeType, proxy, property)
	//	{
	//		this.OldValue = oldValue;
	//		this.NewValue = newValue;
	//	}
	//}
}
