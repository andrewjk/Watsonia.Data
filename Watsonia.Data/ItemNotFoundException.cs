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
		public object ID { get; set; }

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
		/// Initializes a new instance of the <see cref="ItemNotFoundException" /> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public ItemNotFoundException(string message, Exception innerException)
			: base(message, innerException)
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

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemNotFoundException" /> class
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The inner exception.</param>
		/// <param name="id">The ID of the item that was not found.</param>
		public ItemNotFoundException(string message, Exception innerException, object id)
			: this(message, innerException)
		{
			this.ID = id;
		}
	}
}
