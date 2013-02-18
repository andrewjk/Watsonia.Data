using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberFloorFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberFloorFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
