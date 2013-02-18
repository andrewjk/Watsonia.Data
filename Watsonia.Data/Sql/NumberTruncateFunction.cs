using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberTruncateFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberTruncateFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
