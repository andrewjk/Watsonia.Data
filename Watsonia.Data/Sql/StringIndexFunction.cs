using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringIndexFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringIndexFunction;
			}
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public StatementPart StringToFind
		{
			get;
			set;
		}

		public StatementPart StartIndex
		{
			get;
			set;
		}
	}
}
