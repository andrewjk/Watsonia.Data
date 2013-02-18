using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// A collection of conditions.
	/// </summary>
	public sealed class ConditionCollection : List<Condition>
	{
		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append("(");
			for (int i = 0; i < this.Count; i++)
			{
				if (i > 0)
				{
					b.Append(" ");
					b.Append(this[i].Relationship.ToString());
					b.Append(" ");
				}
				b.Append(this[i].ToString());
			}
			b.Append(")");
			return b.ToString();
		}
	}
}
