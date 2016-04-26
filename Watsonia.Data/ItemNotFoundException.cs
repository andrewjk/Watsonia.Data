using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// An item queried from the database was not found.
	/// </summary>
	[Serializable]
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

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemNotFoundException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		public ItemNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			if (info != null)
			{
				this.ID = info.GetValue("id", typeof(object));
			}
		}

		/// <summary>
		/// When serializing, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> 
		/// with information about the exception. </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds 
		/// the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic) </exception>
		[SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			// 'info' guaranteed to be non-null (base.GetObjectData() will throw an ArugmentNullException if it is)
			info.AddValue("id", this.ID);
		}
	}
}
