using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class ConditionalCase : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.ConditionalCase;
			}
		}

		public StatementPart Test
		{
			get;
			set;
		}

		public StatementPart IfTrue
		{
			get;
			set;
		}

		public StatementPart IfFalse
		{
			get;
			set;
		}
	}
}
