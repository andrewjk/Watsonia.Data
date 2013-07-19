using System;
using System.Collections.Generic;
using System.Linq;
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

		internal Insert()
		{
		}

		public static Insert Into(string tableName)
		{
			return Insert.Into(new Table(tableName));
		}

		public static Insert Into(Table table)
		{
			return new Insert() { Target = table };
		}

		internal static Insert<T> Into<T>()
		{
			return new Insert<T>();
		}

		public Insert Value(string columnName, object value)
		{
			this.SetValues.Add(new SetValue(columnName, value));
			return this;
		}

		public Insert Columns(params string[] columnNames)
		{
			this.TargetFields.AddRange(columnNames.Select(cn => new Column(cn)));
			return this;
		}

		public Insert Select(Select statement)
		{
			this.Source = statement;
			return this;
		}
	}
}
