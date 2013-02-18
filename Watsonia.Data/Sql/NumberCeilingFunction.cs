using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberCeilingFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberCeilingFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
