using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data
{
	/// <summary>
	/// A class that contains generic event data.
	/// </summary>
	/// <typeparam name="T">The type of the event data to store.</typeparam>
	public class EventArgs<T> : EventArgs
	{
		/// <summary>
		/// Gets or sets the parameter.
		/// </summary>
		/// <value>
		/// The parameter.
		/// </value>
		public T Parameter { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EventArgs{T}" /> class.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		public EventArgs(T parameter)
		{
			this.Parameter = parameter;
		}
	}
}
