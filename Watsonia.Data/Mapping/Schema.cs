using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Mapping
{
	/// <summary>
	/// Contains mappings from objects to the database.
	/// </summary>
	public class Schema
	{
		/// <summary>
		/// Gets the tables that should exist in the database.
		/// </summary>
		/// <value>
		/// The tables.
		/// </value>
		public IList<MappedTable> Tables { get; } = new List<MappedTable>();

		/// <summary>
		/// Gets the views that should exist in the database.
		/// </summary>
		/// <value>
		/// The views.
		/// </value>
		public IList<MappedView> Views { get; } = new List<MappedView>();

		/// <summary>
		/// Gets the stored procedures that should exist in the database.
		/// </summary>
		/// <value>
		/// The procedures.
		/// </value>
		public IList<MappedProcedure> Procedures { get; } = new List<MappedProcedure>();

		/// <summary>
		/// Gets the user-defined functions that should exist in the database.
		/// </summary>
		/// <value>
		/// The functions.
		/// </value>
		public IList<MappedFunction> Functions { get; } = new List<MappedFunction>();
	}
}
