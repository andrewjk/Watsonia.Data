using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data
{
	/// <summary>
	/// Represents a mapping from a relationship to a foreign key constraint in the database.
	/// </summary>
	public class MappedRelationship
	{
		/// <summary>
		/// Gets or sets the name of the constraint.
		/// </summary>
		/// <value>
		/// The name of the foreign key constraint.
		/// </value>
		public string ConstraintName { get; private set; }

		/// <summary>
		/// Gets or sets the type of the foreign table.
		/// </summary>
		/// <value>
		/// The type of the foreign table.
		/// </value>
		public Type ForeignTableType { get; private set; }

		/// <summary>
		/// Gets or sets the name of the foreign table.
		/// </summary>
		/// <value>
		/// The name of the foreign table.
		/// </value>
		public string ForeignTableName { get; private set; }

		/// <summary>
		/// Gets or sets the name of the foreign table column.
		/// </summary>
		/// <value>
		/// The name of the foreign table column.
		/// </value>
		public string ForeignTableColumnName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedRelationship" /> class.
		/// </summary>
		/// <param name="constraintName">The name of the foreign key constraint.</param>
		/// <param name="foreignTableType">The type of the foreign table.</param>
		/// <param name="foreignTableName">The name of the foreign table.</param>
		/// <param name="foreignTableColumnName">The name of the foreign table column.</param>
		public MappedRelationship(string constraintName, Type foreignTableType, string foreignTableName, string foreignTableColumnName)
		{
			this.ConstraintName = constraintName;
			this.ForeignTableType = foreignTableType;
			this.ForeignTableName = foreignTableName;
			this.ForeignTableColumnName = foreignTableColumnName;
		}
	}
}
