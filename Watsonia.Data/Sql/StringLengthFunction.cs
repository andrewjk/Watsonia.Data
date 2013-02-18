using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringLengthFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringLengthFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}
	}
}
