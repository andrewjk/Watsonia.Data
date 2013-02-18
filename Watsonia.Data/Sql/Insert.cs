using System;
using System.Collections.Generic;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class Insert : Statement
	{
		private readonly List<SetValue> _setValues = new List<SetValue>();
		private readonly List<Column> _targetFields = new List<Column>();

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
			get
			{
				return _setValues;
			}
		}

		public List<Column> TargetFields
		{
			get
			{
				return _targetFields;
			}
		}

		public Select Source
		{
			get;
			set;
		}

		public static Insert Into(string tableName)
		{
			return new Insert(tableName);
		}

		private Insert(string tableName)
		{
			this.Target = new Table(tableName);
		}

		public Insert Value(string columnName, object value)
		{
			this.SetValues.Add(new SetValue(columnName, value));
			return this;
		}

		public Insert Columns(params string[] columnNames)
		{
			this.TargetFields.AddRange(Array.ConvertAll(columnNames, name => new Column(name)));
			return this;
		}

		public Insert Select(Select statement)
		{
			this.Source = statement;
			return this;
		}
	}
}
