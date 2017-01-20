using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringToUpperFunction : Field
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringToUpperFunction;
			}
		}

		public StatementPart Argument { get; set; }

		public override string ToString()
		{
			return "ToUpper(" + this.Argument.ToString() + ")";
		}
	}
}
