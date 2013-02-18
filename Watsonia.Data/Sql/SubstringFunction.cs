using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class SubstringFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.SubstringFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public StatementPart StartIndex
		{
			get;
			set;
		}

		public StatementPart Length
		{
			get;
			set;
		}
	}
}
