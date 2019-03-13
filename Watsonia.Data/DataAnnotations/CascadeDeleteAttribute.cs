using System;

namespace Watsonia.Data.DataAnnotations
{
	/// <summary>
	/// An attribute to place on entity properties when the items they contain should be saved with the entity.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class CascadeDeleteAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether the item contained in this property should be deleted with its parent.
		/// </summary>
		/// <value>
		///   <c>true</c> if the property should be cascaded; otherwise, <c>false</c>.
		/// </value>
		public bool ShouldCascade { get; set; } = true;
	}
}
