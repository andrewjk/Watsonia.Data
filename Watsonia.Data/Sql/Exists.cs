using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class Exists : Condition
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Exists;
			}
		}

		public Select Select
		{
			get;
			set;
		}
	}
}
