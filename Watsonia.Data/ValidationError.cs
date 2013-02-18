
namespace Watsonia.Data
{
	/// <summary>
	/// A validation error that occurs when an entity is in an invalid state.
	/// </summary>
	public class ValidationError
	{
		/// <summary>
		/// Gets or sets the name of the property that is invalid.
		/// </summary>
		/// <value>
		/// The name of the property.
		/// </value>
		public string PropertyName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the error.
		/// </summary>
		/// <value>
		/// The name of the error.
		/// </value>
		public string ErrorName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets an error message to display to the end user.
		/// </summary>
		/// <value>
		/// The error message.
		/// </value>
		public string ErrorMessage
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationError" /> class.
		/// </summary>
		/// <param name="propertyName">The name of the property that is invalid.</param>
		/// <param name="errorName">The name of the error.</param>
		/// <param name="errorMessage">The error message to display to the end user.</param>
		public ValidationError(string propertyName, string errorName, string errorMessage)
		{
			this.PropertyName = propertyName;
			this.ErrorName = errorName;
			this.ErrorMessage = errorMessage;
		}
	}
}
