using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberPowerFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberPowerFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public StatementPart Power
		{
			get;
			set;
		}
	}
}
