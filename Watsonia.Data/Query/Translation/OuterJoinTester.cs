using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Query.Expressions;

namespace Watsonia.Data.Query.Translation
{
	// TODO: This was copied from QueryLanguage, is it needed?
	internal class OuterJoinTester
	{
		public static Expression GetOuterJoinTest(SelectExpression select)
		{
			// if the column is used in the join condition (equality test)
			// if it is null in the database then the join test won't match (null != null) so the row won't appear
			// we can safely use this existing column as our test to determine if the outer join produced a row

			// find a column that is used in equality test
			var aliases = DeclaredAliasGatherer.Gather(select.From);
			var joinColumns = JoinColumnGatherer.Gather(aliases, select).ToList();
			if (joinColumns.Count > 0)
			{
				// prefer one that is already in the projection list.
				foreach (var jc in joinColumns)
				{
					foreach (var col in select.Columns)
					{
						if (jc.Equals(col.Expression))
						{
							return jc;
						}
					}
				}
				return joinColumns[0];
			}

			// fall back to introducing a constant
			return Expression.Constant(1, typeof(int?));
		}

		public static ProjectionExpression AddOuterJoinTest(ProjectionExpression proj)
		{
			var test = GetOuterJoinTest(proj.SelectExpression);
			var select = proj.SelectExpression;
			ColumnExpression testCol = null;
			// look to see if test expression exists in columns already
			foreach (var col in select.Columns)
			{
				if (test.Equals(col.Expression))
				{
					testCol = new ColumnExpression(test.Type, select.Alias, col.Name);
					break;
				}
			}
			if (testCol == null)
			{
				// add expression to projection
				testCol = test as ColumnExpression;
				string colName = (testCol != null) ? testCol.Name : "Test";
				colName = proj.SelectExpression.Columns.GetAvailableColumnName(colName);
				select = select.AddColumn(new ColumnDeclaration(colName, test, test.Type));
				testCol = new ColumnExpression(test.Type, select.Alias, colName);
			}
			var newProjector = new OuterJoinedExpression(testCol, proj.Projector);
			return new ProjectionExpression(select, newProjector, proj.Aggregator);
		}
	}
}
