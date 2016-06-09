using System;
using System.Collections.Generic;
using System.Linq;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class UpdateStatement : Statement
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Update;
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

		public List<ConditionExpression> Conditions
		{
			get;
			private set;
		}

		internal UpdateStatement()
		{
			this.SetValues = new List<SetValue>();
			this.Conditions = new List<ConditionExpression>();
		}
	}
}
