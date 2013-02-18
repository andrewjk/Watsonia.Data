using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberRoundFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberRoundFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public StatementPart Precision
		{
			get;
			set;
		}
	}
}
