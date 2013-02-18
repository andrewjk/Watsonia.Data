using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class DateNewFunction : StatementPart
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.DateNewFunction;
			}
		}

		public StatementPart Year
		{
			get;
			set;
		}

		public StatementPart Month
		{
			get;
			set;
		}

		public StatementPart Day
		{
			get;
			set;
		}

		public StatementPart Hour
		{
			get;
			set;
		}

		public StatementPart Minute
		{
			get;
			set;
		}

		public StatementPart Second
		{
			get;
			set;
		}
	}
}
