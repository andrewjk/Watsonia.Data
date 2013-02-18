using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class RowNumber : StatementPart
	{
		private readonly List<OrderByExpression> _orderByFields = new List<OrderByExpression>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.RowNumber;
			}
		}

		public List<OrderByExpression> OrderByFields
		{
			get
			{
				return _orderByFields;
			}
		}

		public RowNumber(params OrderByExpression[] orderByFields)
		{
			this.OrderByFields.AddRange(orderByFields);
		}

		public override string ToString()
		{
			return "RowNumber";
		}
	}
}
