using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	// TODO: Use this?!
	public sealed class Parameter : SourceExpression
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Parameter;
			}
		}

		public string Name { get; private set; }

		public object Value { get; private set; }

		public Parameter(string name, object value)
		{
			this.Name = name;
			this.Value = value;
		}

		public override string ToString()
		{
			if (this.Name.StartsWith("@n"))
			{
				// It's a named value expression, and inscrutable to the user
				return this.Name;
			}
			else if (this.Value is string || this.Value is char)
			{
				return string.Format("{0} ('{1}')", this.Name, this.Value);
			}
			else
			{
				return string.Format("{0} ({1})", this.Name, this.Value);
			}
		}
	}
}
