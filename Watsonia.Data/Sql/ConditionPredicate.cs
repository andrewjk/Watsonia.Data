using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	// TODO: What even is this
	public sealed class ConditionPredicate : SourceExpression
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.ConditionPredicate;
			}
		}

		public StatementPart Predicate
		{
			get;
			set;
		}
	}
}
