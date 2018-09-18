using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	public sealed class ValidationException : Exception
	{
		/// <summary>
		/// Gets the validation errors.
		/// </summary>
		/// <value>
		/// The validation errors.
		/// </value>
		public List<ValidationError> ValidationErrors { get; } = new List<ValidationError>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationException"/> class.
		/// </summary>
		public ValidationException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ValidationException(string message)
			: base(message)
		{
		}
	}
}
