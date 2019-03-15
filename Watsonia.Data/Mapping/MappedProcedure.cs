﻿using System.Collections.Generic;
using System.Data.Common;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Mapping
{
	/// <summary>
	/// Represents a mapping from a class to a stored procedure in the database.
	/// </summary>
	public class MappedProcedure
	{
		/// <summary>
		/// Gets or sets name of the procedure.
		/// </summary>
		/// <value>
		/// The name of the procedure.
		/// </value>
		public string Name { get; }

		/// <summary>
		/// Gets or sets the parameters for the procedure.
		/// </summary>
		/// <value>
		/// The parameters.
		/// </value>
		public IList<MappedParameter> Parameters { get; set; } = new List<MappedParameter>();

		/// <summary>
		/// Gets or sets the statement that the procedure is built from.
		/// </summary>
		/// <value>
		/// The statement.
		/// </value>
		public Statement Statement { get; set; }

		/// <summary>
		/// Gets or sets the statement command text that exists for the procedure.
		/// </summary>
		/// <value>
		/// The statement command text.
		/// </value>
		public string StatementText { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedProcedure" /> class.
		/// </summary>
		/// <param name="name">The name of the procedure.</param>
		public MappedProcedure(string name)
		{
			this.Name = name;
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
