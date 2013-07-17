using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// An expression that can be used in the field list of a select statement.
	/// </summary>
	public abstract class SourceExpression : StatementPart
	{
		////public override StatementPartType PartType
		////{
		////	get
		////	{
		////		return StatementPartType.SourceExpression;
		////	}
		////}

		////public StatementPart Field
		////{
		////	get;
		////	internal set;
		////}

		// TODO: Should this just be a string?
		public string Alias
		{
			get;
			set;
		}

		////internal SourceExpression()
		////{
		////}

		////public SourceExpression(StatementPart field)
		////{
		////	this.Field = field;
		////}

		////public SourceExpression(string columnName)
		////{
		////	this.Field = new Column(columnName);
		////}

		////public override string ToString()
		////{
		////	if (!string.IsNullOrEmpty(this.Alias))
		////	{
		////		return string.Format("{0} as {1}", this.Field, this.Alias);
		////	}
		////	else
		////	{
		////		return this.Field.ToString();
		////	}
		////}
	}
}
