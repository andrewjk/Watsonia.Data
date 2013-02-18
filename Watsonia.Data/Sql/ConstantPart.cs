using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// A statement part containing a constant value.
	/// </summary>
	public sealed class ConstantPart : StatementPart
	{
		/// <summary>
		/// Gets the type of the statement part.
		/// </summary>
		/// <value>
		/// The type of the part.
		/// </value>
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.ConstantPart;
			}
		}

		/// <summary>
		/// Gets the constant value.
		/// </summary>
		/// <value>
		/// The constant value.
		/// </value>
		public object Value
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConstantPart" /> class.
		/// </summary>
		/// <param name="value">The constant value.</param>
		public ConstantPart(object value)
		{
			this.Value = value;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return (this.Value != null) ? this.Value.ToString() : "Null";
		}
	}
}
