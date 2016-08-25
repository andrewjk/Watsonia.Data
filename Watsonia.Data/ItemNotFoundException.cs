using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// An item queried from the database was not found.
	/// </summary>
	public class ItemNotFoundException : Exception
	{
		/// <summary>
		/// Gets or sets the ID of the item that was not found.
		/// </summary>
		/// <value>The ID of the item.</value>
		public object ID
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemNotFoundException"/> class.
		/// </summary>
		public ItemNotFoundException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemNotFoundException" /> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ItemNotFoundException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemNotFoundException" /> class
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="id">The ID of the item that was not found.</param>
		public ItemNotFoundException(string message, object id)
			: this(message)
		{
			this.ID = id;
		}
	}
}
