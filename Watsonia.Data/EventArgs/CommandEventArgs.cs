using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.EventArgs
{
	/// <summary>
	/// A class that contains command event data.
	/// </summary>
	public class CommandEventArgs : System.EventArgs
	{
		/// <summary>
		/// Gets or sets the command.
		/// </summary>
		/// <value>
		/// The command.
		/// </value>
		public DbCommand Command { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandEventArgs"/> class.
		/// </summary>
		/// <param name="command">The command.</param>
		public CommandEventArgs(DbCommand command)
		{
			this.Command = command;
		}
	}
}
