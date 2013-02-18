using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberNegateFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberNegateFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
