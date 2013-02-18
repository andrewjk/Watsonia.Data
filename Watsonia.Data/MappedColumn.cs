using System;

namespace Watsonia.Data
{
	/// <summary>
	/// Represents a mapping from a property to a column in the database.
	/// </summary>
	public class MappedColumn
	{
		/// <summary>
		/// Gets or sets the name of the column.
		/// </summary>
		/// <value>
		/// The name of the column.
		/// </value>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the CLR type of the column.
		/// </summary>
		/// <value>
		/// The CLR type of the column.
		/// </value>
		public Type ColumnType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the maximum length of data in the column.
		/// </summary>
		/// <value>
		/// The maximum length of data in the column.
		/// </value>
		public int MaxLength
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the relationship.
		/// </summary>
		/// <value>
		/// The relationship.
		/// </value>
		public MappedRelationship Relationship
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the default value.
		/// </summary>
		/// <value>
		/// The default value.
		/// </value>
		public object DefaultValue
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the default value constraint.
		/// </summary>
		/// <value>
		/// The name of the default value constraint.
		/// </value>
		public string DefaultValueConstraintName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this column is the primary key.
		/// </summary>
		/// <value>
		/// <c>true</c> if this column is the primary key; otherwise, <c>false</c>.
		/// </value>
		public bool IsPrimaryKey
		{
			get;
			set;
		}

		/// <summary>
		/// Gets a value indicating whether this column allows null values.
		/// </summary>
		/// <value>
		/// <c>true</c> if this column allows nulls; otherwise, <c>false</c>.
		/// </value>
		public bool AllowNulls
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedColumn" /> class.
		/// </summary>
		/// <param name="name">The name of the column.</param>
		/// <param name="columnType">Type of the column.</param>
		public MappedColumn(string name, Type columnType, string defaultValueConstraintName)
		{
			this.Name = name;
			this.ColumnType = columnType;
			this.DefaultValueConstraintName = defaultValueConstraintName;
			this.AllowNulls = DefaultAllowNulls();
		}

		private bool DefaultAllowNulls()
		{
			if (this.ColumnType == typeof(string))
			{
				// String columns are not nullable to avoid the hassles of always having to check
				// for null before doing anything with their contents
				return false;
			}
			else if (!this.ColumnType.IsValueType || Nullable.GetUnderlyingType(this.ColumnType) != null)
			{
				// Columns which contain a reference type or a nullable value type allow nulls
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
