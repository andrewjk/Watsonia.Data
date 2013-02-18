﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// A table in the database.
	/// </summary>
	public sealed class Table : StatementPart
	{
		private readonly List<string> _includePaths = new List<string>();

		/// <summary>
		/// Gets the type of the statement part.
		/// </summary>
		/// <value>
		/// The type of the part.
		/// </value>
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Table;
			}
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		/// <value>
		/// The name of the table.
		/// </value>
		public string Name
		{
			get;
			internal set;
		}

		public string Alias
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the paths of related items and collections to include when loading data from this table.
		/// </summary>
		/// <value>
		/// The include paths.
		/// </value>
		internal List<string> IncludePaths
		{
			get
			{
				return _includePaths;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Table" /> class.
		/// </summary>
		internal Table()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Table" /> class.
		/// </summary>
		/// <param name="name">The name of the table.</param>
		public Table(string name)
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
			return this.Name + (!string.IsNullOrEmpty(this.Alias) ? " As " + this.Alias : "");
		}
	}
}
