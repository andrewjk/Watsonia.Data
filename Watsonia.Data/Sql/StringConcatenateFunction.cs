using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class StringConcatenateFunction : StatementPart
	{
		private readonly List<StatementPart> _arguments = new List<StatementPart>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.StringConcatenateFunction;
			}
		}

		public List<StatementPart> Arguments
		{
			get
			{
				return _arguments;
			}
		}
	}
}
