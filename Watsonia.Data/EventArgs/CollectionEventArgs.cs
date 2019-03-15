using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains collection event data.
	/// </summary>
	public class CollectionEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets or sets the collection.
		/// </summary>
		/// <value>
		/// The collection.
		/// </value>
		public IEnumerable Collection { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionEventArgs"/> class.
		/// </summary>
		/// <param name="collection">The collection.</param>
		public CollectionEventArgs(IEnumerable collection)
		{
			this.Collection = collection;
		}
	}
}
