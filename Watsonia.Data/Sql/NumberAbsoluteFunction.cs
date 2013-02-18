using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberAbsoluteFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberAbsoluteFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
