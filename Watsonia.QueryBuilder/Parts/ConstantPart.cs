using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Watsonia.QueryBuilder
{
	/// <summary>
	/// A statement part containing a constant value.
	/// </summary>
	public sealed class ConstantPart : SourceExpression
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
		public object Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConstantPart" /> class.
		/// </summary>
		/// <param name="value">The constant value.</param>
		public ConstantPart(object value)
		{
			this.Value = value;
		}

		/// <summary>
		/// Returns a <see cref="string" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="string" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			var b = new StringBuilder();
			if (this.Value == null)
			{
				b.Append("Null");
			}
			else if (this.Value is string || this.Value is char || this.Value is DateTime)
			{
				b.Append("'");
				b.Append(FormatValue(this.Value));
				b.Append("'");
			}
			else if (this.Value is IEnumerable enumerable)
			{
				b.Append("{ ");
				var values = new List<string>();
				foreach (var o in enumerable)
				{
					if (o == null)
					{
						values.Add("Null");
					}
                    else if (o is string || o is char || o is DateTime)
                    {
						values.Add("'" + FormatValue(o) + "'");
                    }
                    else
                    {
						values.Add(FormatValue(o));
					}
				}
				b.Append(string.Join(", ", values));
				b.Append(" }");
			}
			else
			{
				b.Append(FormatValue(this.Value));
			}
			if (!string.IsNullOrEmpty(this.Alias))
			{
				b.Append(" As ");
				b.Append(this.Alias);
			}
			return b.ToString();
		}

		/// <summary>
		/// Formats a value for display in a statement, independently of the current culture.
		/// </summary>
		private static string FormatValue(object value)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}
	}
}
