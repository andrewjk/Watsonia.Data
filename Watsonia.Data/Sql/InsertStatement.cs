using System;
using System.Collections.Generic;
using System.Linq;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class InsertStatement : Statement
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Insert;
			}
		}

		public Table Target
		{
			get;
			set;
		}

		public List<SetValue> SetValues
		{
			get;
			private set;
		}

		public List<Column> TargetFields
		{
			get;
			private set;
		}

		public SelectStatement Source
		{
			get;
			set;
		}

		internal InsertStatement()
		{
			this.SetValues = new List<SetValue>();
			this.TargetFields = new List<Column>();
		}
	}
}
