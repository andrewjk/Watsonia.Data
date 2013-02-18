using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringTrimFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringTrimFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
