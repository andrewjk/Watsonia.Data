using System.Collections.Generic;

namespace Watsonia.Data
{
	/// <summary>
	/// Represents a mapping from a class to a view in the database.
	/// </summary>
	public class MappedView
	{
		/// <summary>
		/// Gets or sets name of the view.
		/// </summary>
		/// <value>
		/// The name of the view.
		/// </value>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the select statement that the view is built from.
		/// </summary>
		/// <value>
		/// The select statement.
		/// </value>
		public Statement SelectStatement
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the select statement command text that exists for the view.
		/// </summary>
		/// <value>
		/// The select statement command text.
		/// </value>
		public string SelectStatementText
		{
			get;
			set;
		}
	
		/// <summary>
		/// Initializes a new instance of the <see cref="MappedView" /> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		public MappedView(string name)
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
