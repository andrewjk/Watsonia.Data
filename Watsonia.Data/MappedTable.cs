using System.Collections.Generic;

namespace Watsonia.Data
{
	/// <summary>
	/// Represents a mapping from a class to a table in the database.
	/// </summary>
	public class MappedTable
	{

		/// <summary>
		/// Gets or sets name of the table.
		/// </summary>
		/// <value>
		/// The name of the table.
		/// </value>
		public string Name { get; private set; }

		/// <summary>
		/// Gets or sets the name of the primary key column.
		/// </summary>
		/// <value>
		/// The name of the primary key column.
		/// </value>
		public string PrimaryKeyColumnName { get; set; }

		/// <summary>
		/// Gets or sets the name of the primary key constraint.
		/// </summary>
		/// <value>
		/// The name of the primary key constraint.
		/// </value>
		public string PrimaryKeyConstraintName { get; set; }

		/// <summary>
		/// Gets the columns in the table.
		/// </summary>
		/// <value>
		/// The columns.
		/// </value>
		public List<MappedColumn> Columns { get; } = new List<MappedColumn>();

		/// <summary>
		/// Gets any values that should be created in the table.
		/// </summary>
		/// <value>
		/// The values.
		/// </value>
		public List<Dictionary<string, object>> Values { get; } = new List<Dictionary<string, object>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedTable" /> class.
		/// </summary>
		/// <param name="name">The name of the table.</param>
		public MappedTable(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedTable" /> class.
		/// </summary>
		/// <param name="name">The name of the table.</param>
		/// <param name="primaryKeyColumnName">Name of the primary key column.</param>
		/// <param name="primaryKeyConstraintName">Name of the primary key constraint.</param>
		public MappedTable(string name, string primaryKeyColumnName, string primaryKeyConstraintName)
		{
			this.Name = name;
			this.PrimaryKeyColumnName = primaryKeyColumnName;
			this.PrimaryKeyConstraintName = primaryKeyConstraintName;
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
