using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains item event data.
	/// </summary>
	public class ItemEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets or sets the item.
		/// </summary>
		/// <value>
		/// The item.
		/// </value>
		public IDynamicProxy Item { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemEventArgs"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		public ItemEventArgs(IDynamicProxy item)
		{
			this.Item = item;
		}
	}
}
