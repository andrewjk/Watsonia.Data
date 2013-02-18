using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	// TODO: Use this?!
	public sealed class Parameter : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Parameter;
			}
		}

		public string Name
		{
			get;
			private set;
		}

		public object Value
		{
			get;
			private set;
		}

		public Parameter(string name, object value)
		{
			this.Name = name;
			this.Value = value;
		}

		public override string ToString()
		{
			return string.Format("{0} ({1})", this.Name, this.Value);
		}
	}
}
