using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains value event data.
	/// </summary>
	public class ValueEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		public object Value { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueEventArgs"/> class.
		/// </summary>
		/// <param name="value">The value.</param>
		public ValueEventArgs(object value)
		{
			this.Value = value;
		}
	}
}
