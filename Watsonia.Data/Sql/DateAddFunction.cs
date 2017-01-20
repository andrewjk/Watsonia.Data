using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class DateAddFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.DateAddFunction;
			}
		}

		public DatePart DatePart { get; set; }

		public StatementPart Argument { get; set; }

		public StatementPart Number { get; set; }

		internal DateAddFunction(DatePart datePart)
		{
			this.DatePart = datePart;
		}

		public override string ToString()
		{
			return string.Format("DateAdd({0}, {1}, {2})", this.DatePart, this.Argument, this.Number);
		}
	}
}
