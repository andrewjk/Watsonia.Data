using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringReplaceFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringReplaceFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public StatementPart OldValue
		{
			get;
			set;
		}

		public StatementPart NewValue
		{
			get;
			set;
		}
	}
}
