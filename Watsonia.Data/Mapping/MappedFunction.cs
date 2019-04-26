﻿using System.Collections.Generic;
using System.Data.Common;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Mapping
{
	/// <summary>
	/// Represents a mapping from a class to a user-defined function in the database.
	/// </summary>
	public class MappedFunction
	{
		/// <summary>
		/// Gets or sets name of the function.
		/// </summary>
		/// <value>
		/// The name of the function.
		/// </value>
		public string Name { get; }

		/// <summary>
		/// Gets or sets the parameters for the function.
		/// </summary>
		/// <value>
		/// The parameters.
		/// </value>
		public IList<MappedParameter> Parameters { get; } = new List<MappedParameter>();

		/// <summary>
		/// Gets or sets the statement that the function is built from.
		/// </summary>
		/// <value>
		/// The statement.
		/// </value>
		public Statement Statement { get; set; }

		/// <summary>
		/// Gets or sets the statement command text that exists for the function.
		/// </summary>
		/// <value>
		/// The statement command text.
		/// </value>
		public string StatementText { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MappedFunction" /> class.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		public MappedFunction(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Returns a <see cref="string" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="string" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return this.Name;
		}
	}
}
