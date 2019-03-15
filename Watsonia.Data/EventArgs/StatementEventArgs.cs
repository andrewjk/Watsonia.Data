using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains statement event data.
	/// </summary>
	public class StatementEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets or sets the statement.
		/// </summary>
		/// <value>
		/// The statement.
		/// </value>
		public Statement Statement { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StatementEventArgs"/> class.
		/// </summary>
		/// <param name="statement">The statement.</param>
		public StatementEventArgs(Statement statement)
		{
			this.Statement = statement;
		}
	}
}
