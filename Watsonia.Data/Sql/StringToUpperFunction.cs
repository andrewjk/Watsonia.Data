using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringToUpperFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringToUpperFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
