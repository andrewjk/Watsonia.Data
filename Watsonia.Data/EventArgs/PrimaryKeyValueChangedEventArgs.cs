using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains primary key value event data.
	/// </summary>
	public sealed class PrimaryKeyValueChangedEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		public object Value { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimaryKeyValueChangedEventArgs"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		public PrimaryKeyValueChangedEventArgs(object value)
		{
			this.Value = value;
		}
	}
}
