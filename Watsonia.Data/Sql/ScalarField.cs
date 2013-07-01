﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// A field containing a select statement that returns a single value.
	/// </summary>
	public sealed class ScalarField : SourceExpression
	{
		/// <summary>
		/// Gets the type of the statement part.
		/// </summary>
		/// <value>
		/// The type of the part.
		/// </value>
		/// <exception cref="System.NotImplementedException"></exception>
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.SelectField;
			}
		}

		/// <summary>
		/// Gets the select statement.
		/// </summary>
		/// <value>
		/// The select statement.
		/// </value>
		public Select Select
		{
			get;
			internal set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarField"/> class.
		/// </summary>
		internal ScalarField()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScalarField"/> class.
		/// </summary>
		/// <param name="select">The select statement.</param>
		public ScalarField(Select select)
		{
			this.Select = select;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append(this.Select.ToString());
			if (!string.IsNullOrEmpty(this.Alias))
			{
				b.Append(" As ");
				b.Append(this.Alias);
			}
			return b.ToString();
		}
	}
}
