
namespace Watsonia.Data
{
	/// <summary>
	/// A validation error that occurs when an entity is in an invalid state.
	/// </summary>
	public class ValidationError
	{
		/// <summary>
		/// Gets or sets the ID of the item that is invalid.
		/// </summary>
		/// <value>
		/// The ID of the item.
		/// </value>
		public object ItemID { get; set; }

		/// <summary>
		/// Gets or sets the name of the item that is invalid.
		/// </summary>
		/// <value>
		/// The name of the item.
		/// </value>
		public string ItemName { get; set; }

		/// <summary>
		/// Gets or sets the name of the property that is invalid.
		/// </summary>
		/// <value>
		/// The name of the property.
		/// </value>
		public string PropertyName { get; set; }

		/// <summary>
		/// Gets or sets the name of the error.
		/// </summary>
		/// <value>
		/// The name of the error.
		/// </value>
		public string ErrorName { get; set; }

		/// <summary>
		/// Gets or sets an error message to display to the end user.
		/// </summary>
		/// <value>
		/// The error message.
		/// </value>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidationError" /> class.
		/// </summary>
		/// <param name="itemID">The ID of the item that is invalid.</param>
		/// <param name="itemName">The name of the item that is invalid.</param>
		/// <param name="propertyName">The name of the property that is invalid.</param>
		/// <param name="errorName">The name of the error.</param>
		/// <param name="errorMessage">The error message to display to the end user.</param>
		public ValidationError(object itemID, string itemName, string propertyName, string errorName, string errorMessage)
		{
			this.ItemName = itemName;
			this.PropertyName = propertyName;
			this.ErrorName = errorName;
			this.ErrorMessage = errorMessage;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return $"{this.ItemName}.{this.PropertyName}: {this.ErrorName} ({this.ErrorMessage})";
		}
	}
}
