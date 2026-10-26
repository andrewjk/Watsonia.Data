using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.QueryBuilder
{
	/// <summary>
	/// The starting point for fluently creating select statements.
	/// </summary>
	public static partial class Select
	{
		/// <summary>
		/// Creates a select statement with the name of the table that records should be selected from.
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="alias">The alias.</param>
		/// <param name="schema">The schema.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement From(string tableName, string alias = null, string schema = null)
		{
			return Select.From(new Table(tableName, alias, schema));
		}

		/// <summary>
		/// Creates a select statement with the table that records should be selected from.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement From(Table table)
		{
			return new SelectStatement() { Source = table };
		}

		/// <summary>
		/// Creates a select statement from a join.
		/// </summary>
		/// <param name="join">The join.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement From(Join join)
		{
			return new SelectStatement() { Source = join };
		}

		/// <summary>
		/// Creates a select statement from a statement part.
		/// </summary>
		/// <param name="part">The part.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement From(StatementPart part)
		{
			return new SelectStatement() { Source = part };
		}

		/// <summary>
		/// Adds a join to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="join">The join.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Join(this SelectStatement select, Join join)
		{
			select.SourceJoins.Add(join);
			return select;
		}

		/// <summary>
		/// Adds a join to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="leftTableName">Name of the left table.</param>
		/// <param name="leftColumnName">Name of the left column.</param>
		/// <param name="rightTableName">Name of the right table.</param>
		/// <param name="rightColumnName">Name of the right column.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Join(this SelectStatement select, string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			select.SourceJoins.Add(new Join(tableName, leftTableName, leftColumnName, rightTableName, rightColumnName));
			return select;
		}

		/// <summary>
		/// Adds a join to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="leftTableName">Name of the left table.</param>
		/// <param name="leftColumnName">Name of the left column.</param>
		/// <param name="rightTableName">Name of the right table.</param>
		/// <param name="rightColumnName">Name of the right column.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Join(this SelectStatement select, JoinType joinType, string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			select.SourceJoins.Add(new Join(joinType, tableName, leftTableName, leftColumnName, rightTableName, rightColumnName));
			return select;
		}

		/// <summary>
		/// Adds a join to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="table">The table.</param>
		/// <param name="leftColumn">The left column.</param>
		/// <param name="rightColumn">The right column.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Join(this SelectStatement select, Table table, Column leftColumn, Column rightColumn)
		{
			select.SourceJoins.Add(new Join(table, leftColumn, rightColumn));
			return select;
		}

		/// <summary>
		/// Adds a join to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="joinType">Type of the join.</param>
		/// <param name="table">The table.</param>
		/// <param name="leftColumn">The left column.</param>
		/// <param name="rightColumn">The right column.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Join(this SelectStatement select, JoinType joinType, Table table, Column leftColumn, Column rightColumn)
		{
			select.SourceJoins.Add(new Join(joinType, table, leftColumn, rightColumn));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Columns(this SelectStatement select, params string[] columnNames)
		{
			select.SourceFields.AddRange(columnNames.Select(cn => new Column(cn)));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Columns(this SelectStatement select, params SourceExpression[] columns)
		{
			select.SourceFields.AddRange(columns);
			return select;
		}

		/// <summary>
		/// Adds a list of tables to select columns from.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="tableNames">The table names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement ColumnsFrom(this SelectStatement select, params string[] tableNames)
		{
			select.SourceFieldsFrom.AddRange(tableNames.Select(tn => new Table(tn)));
			return select;
		}

		/// <summary>
		/// Adds a list of tables to select columns from.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="tables">The tables.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement ColumnsFrom(this SelectStatement select, params Table[] tables)
		{
			select.SourceFieldsFrom.AddRange(tables);
			return select;
		}

		/// <summary>
		/// Adds a COUNT aggregate to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Count(this SelectStatement select)
		{
			select.SourceFields.Add(new Aggregate(AggregateType.Count, new Column("*")));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to COUNT to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Count(this SelectStatement select, params Column[] columns)
		{
			if (columns.Any())
			{
				select.SourceFields.AddRange(columns.Select(cn => new Aggregate(AggregateType.Count, cn)));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Count, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to COUNT to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Count(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Count, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Count, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to SUM to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Sum(this SelectStatement select, params Column[] columns)
		{
			if (columns.Any())
			{
				select.SourceFields.AddRange(columns.Select(cn => new Aggregate(AggregateType.Sum, cn)));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Sum, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to SUM to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Sum(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Sum, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Sum, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a SUM (DISTINCT) aggregate to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement SumDistinct(this SelectStatement select, params Column[] columns)
		{
			foreach (var column in columns)
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Sum, column) { IsDistinct = true });
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to MIN to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Min(this SelectStatement select, params Column[] columns)
		{
			if (columns.Any())
			{
				select.SourceFields.AddRange(columns.Select(cn => new Aggregate(AggregateType.Min, cn)));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Min, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to MIN to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Min(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Min, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Min, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to MAX to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Max(this SelectStatement select, params Column[] columns)
		{
			if (columns.Any())
			{
				select.SourceFields.AddRange(columns.Select(cn => new Aggregate(AggregateType.Max, cn)));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Max, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to MAX to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Max(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Max, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Max, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to AVERAGE to the select statement.
		/// </summary>
		public static SelectStatement Average(this SelectStatement select, params Column[] columns)
		{
			if (columns.Any())
			{
				select.SourceFields.AddRange(columns.Select(cn => new Aggregate(AggregateType.Average, cn)));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Average, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a list of columns to AVERAGE to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Average(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Average, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Average, new Column("*")));
			}
			return select;
		}

		/// <summary>
		/// Adds a COUNT_BIG aggregate to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement LongCount(this SelectStatement select)
		{
			select.SourceFields.Add(new Aggregate(AggregateType.BigCount, new Column("*")));
			return select;
		}

		/// <summary>
		/// Sets the select statement to select only DISTINCT records.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Distinct(this SelectStatement select)
		{
			select.IsDistinct = true;
			return select;
		}

		/// <summary>
		/// Sets the number of records to skip from the start of the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="skip">The number of records to skip.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Skip(this SelectStatement select, int skip)
		{
			select.StartIndex = skip;
			return select;
		}

		/// <summary>
		/// Sets the number of records to take from the start of the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="take">The number of records to take.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Take(this SelectStatement select, int take)
		{
			select.Limit = take;
			return select;
		}

		/// <summary>
		/// Adds a condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Where(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value));
			return select;
		}

		/// <summary>
		/// Adds a condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="column">The column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Where(this SelectStatement select, SourceExpression column, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(column, op, value));
			return select;
		}

		/// <summary>
		/// Adds a condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="conditions">The conditions.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Where(this SelectStatement select, params ConditionExpression[] conditions)
		{
			select.Conditions.AddRange(conditions);
			return select;
		}

		/// <summary>
		/// Adds a NOT condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement WhereNot(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Not = true });
			return select;
		}

		/// <summary>
		/// Adds a NOT condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="column">The column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement WhereNot(this SelectStatement select, SourceExpression column, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(column, op, value) { Not = true });
			return select;
		}

		/// <summary>
		/// Adds an AND condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement And(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.And });
			return select;
		}

		/// <summary>
		/// Adds an AND condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="column">The column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement And(this SelectStatement select, SourceExpression column, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(column, op, value) { Relationship = ConditionRelationship.And });
			return select;
		}

		/// <summary>
		/// Adds an AND condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="condition">The condition.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement And(this SelectStatement select, ConditionExpression condition)
		{
			condition.Relationship = ConditionRelationship.And;
			select.Conditions.Add(condition);
			return select;
		}

		/// <summary>
		/// Adds an AND condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="conditions">The conditions.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement And(this SelectStatement select, params ConditionExpression[] conditions)
		{
			select.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.And });
			return select;
		}

		/// <summary>
		/// Adds an OR condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Or(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.Or });
			return select;
		}

		/// <summary>
		/// Adds an OR condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="column">The column.</param>
		/// <param name="op">The op.</param>
		/// <param name="value">The value.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Or(this SelectStatement select, SourceExpression column, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(column, op, value) { Relationship = ConditionRelationship.Or });
			return select;
		}

		/// <summary>
		/// Adds an OR condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="condition">The condition.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Or(this SelectStatement select, ConditionExpression condition)
		{
			condition.Relationship = ConditionRelationship.Or;
			select.Conditions.Add(condition);
			return select;
		}

		/// <summary>
		/// Adds an OR condition to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="conditions">The conditions.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Or(this SelectStatement select, params ConditionExpression[] conditions)
		{
			select.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.Or });
			return select;
		}

		/// <summary>
		/// Adds a list of columns to order by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement OrderBy(this SelectStatement select, params string[] columnNames)
		{
			select.OrderByFields.AddRange(columnNames.Select(cn => new OrderByExpression(cn)));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to order by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement OrderBy(this SelectStatement select, params Column[] columns)
		{
			select.OrderByFields.AddRange(columns.Select(c => new OrderByExpression(c)));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to order by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement OrderBy(this SelectStatement select, params OrderByExpression[] columns)
		{
			select.OrderByFields.AddRange(columns);
			return select;
		}

		/// <summary>
		/// Adds a list of columns to order descendingly by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement OrderByDescending(this SelectStatement select, params string[] columnNames)
		{
			select.OrderByFields.AddRange(columnNames.Select(c => new OrderByExpression(c, OrderDirection.Descending)));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to group by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columnNames">The column names.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement GroupBy(this SelectStatement select, params string[] columnNames)
		{
			select.GroupByFields.AddRange(columnNames.Select(c => new Column(c)));
			return select;
		}

		/// <summary>
		/// Adds a list of columns to group by to the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="columns">The columns.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement GroupBy(this SelectStatement select, params Column[] columns)
		{
			select.GroupByFields.AddRange(columns);
			return select;
		}

		/// <summary>
		/// Adds another statement to the select statement as a UNION.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="union">The union.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Union(this SelectStatement select, SelectStatement union)
		{
			select.UnionStatements.Add(union);
			return select;
		}

		/// <summary>
		/// Sets additional paths to include when loading the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="path">The path.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Include(this SelectStatement select, string path)
		{
			select.IncludePaths.Add(path);
			return select;
		}

		/// <summary>
		/// Adds an EXISTS condition for the supplied subquery.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="subquery">The subquery.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement WhereExists(this SelectStatement select, SelectStatement subquery)
		{
			select.Conditions.Add(new Exists() { Select = subquery });
			return select;
		}

		/// <summary>
		/// Adds a NOT EXISTS condition for the supplied subquery.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="subquery">The subquery.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement WhereNotExists(this SelectStatement select, SelectStatement subquery)
		{
			select.Conditions.Add(new Exists() { Select = subquery, Not = true });
			return select;
		}

		/// <summary>
		/// Adds an IN condition for the supplied column and subquery.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="column">The column to check.</param>
		/// <param name="subquery">The subquery that returns the values.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement WhereIn(this SelectStatement select, Column column, SelectStatement subquery)
		{
			select.Conditions.Add(new Condition(column, SqlOperator.IsIn, subquery));
			return select;
		}

		/// <summary>
		/// Limits the select statement to the first record.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement First(this SelectStatement select)
		{
			select.Limit = 1;
			return select;
		}

		/// <summary>
		/// Limits the select statement to the first record.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement FirstOrDefault(this SelectStatement select)
		{
			return select.First();
		}

		/// <summary>
		/// Limits the select statement to the first record in reverse order.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Last(this SelectStatement select)
		{
			select.Reverse();
			select.Limit = 1;
			return select;
		}

		/// <summary>
		/// Limits the select statement to the first record in reverse order.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement LastOrDefault(this SelectStatement select)
		{
			return select.Last();
		}

		/// <summary>
		/// Marks the select statement as a single-record query.
		/// </summary>
		/// <remarks>
		/// SQL cannot enforce a single result, so cardinality is checked when the statement is executed.
		/// </remarks>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Single(this SelectStatement select)
		{
			return select;
		}

		/// <summary>
		/// Marks the select statement as a single-record query.
		/// </summary>
		/// <remarks>
		/// SQL cannot enforce a single result, so cardinality is checked when the statement is executed.
		/// </remarks>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement SingleOrDefault(this SelectStatement select)
		{
			return select;
		}

		/// <summary>
		/// Reverses the order of the select statement.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement Reverse(this SelectStatement select)
		{
			if (select.StartIndex != 0 || select.Limit != 0)
			{
				// Paging has already limited the rows, so reverse them in an outer query
				if (select.SourceFields.Count == 0 && select.SourceFieldsFrom.Count == 0 && select.Source is Table sourceTable)
				{
					// Make sure the inner query selects its columns rather than a placeholder
					var sourceName = string.IsNullOrEmpty(sourceTable.Alias) ? sourceTable.Name : sourceTable.Alias;
					select.SourceFieldsFrom.Add(new Table(sourceName));
				}
				var alias = "t0";
				select.Alias = alias;
				var outer = Select.From(select);
				outer.SourceFieldsFrom.Add(new Table(alias));
				foreach (var order in select.OrderByFields)
				{
					var expression = order.Expression is Column column
						? new Column(alias, column.Name)
						: order.Expression;
					outer.OrderByFields.Add(new OrderByExpression(expression, Flip(order.Direction)));
				}
				return outer;
			}

			foreach (var order in select.OrderByFields)
			{
				order.Direction = Flip(order.Direction);
			}
			return select;
		}

		private static OrderDirection Flip(OrderDirection direction)
		{
			return direction == OrderDirection.Ascending ? OrderDirection.Descending : OrderDirection.Ascending;
		}

		/// <summary>
		/// Creates a scalar statement that reports whether the supplied query has any rows.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The scalar select statement.</returns>
		public static SelectStatement Any(this SelectStatement select)
		{
			return BooleanResult(new Exists() { Select = select });
		}

		/// <summary>
		/// Creates a scalar statement that reports whether the supplied query has no rows.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns>The scalar select statement.</returns>
		public static SelectStatement None(this SelectStatement select)
		{
			return BooleanResult(new Exists() { Select = select, Not = true });
		}

		/// <summary>
		/// Creates a scalar statement that reports whether a value is in the supplied query.
		/// </summary>
		/// <param name="item">The value to look for.</param>
		/// <param name="subquery">The query that returns the values.</param>
		/// <returns>The scalar select statement.</returns>
		public static SelectStatement Contains(object item, SelectStatement subquery)
		{
			return BooleanResult(new Condition(new ConstantPart(item), SqlOperator.IsIn, subquery));
		}

		private static SelectStatement BooleanResult(ConditionExpression test)
		{
			var result = Select.From((StatementPart)null);
			result.SourceFields.Add(new ConditionalCase()
			{
				Test = test,
				IfTrue = new ConstantPart(1),
				IfFalse = new ConstantPart(0)
			});
			return result;
		}

		/// <summary>
		/// Adds a subquery that returns a single value as a column.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <param name="subquery">The subquery.</param>
		/// <param name="alias">The column alias.</param>
		/// <returns>The select statement.</returns>
		public static SelectStatement ColumnSubquery(this SelectStatement select, SelectStatement subquery, string alias = null)
		{
			select.SourceFields.Add(new ScalarField(subquery) { Alias = alias });
			return select;
		}
	}
}
