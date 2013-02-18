using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringToLowerFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringToLowerFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
