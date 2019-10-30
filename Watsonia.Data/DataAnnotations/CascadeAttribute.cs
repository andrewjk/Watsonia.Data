using System;

namespace Watsonia.Data.DataAnnotations
{
	/// <summary>
	/// An attribute to place on entity properties when the items they contain should be saved with the entity.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class CascadeAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether the item contained in this property should be saved with its parent.
		/// </summary>
		/// <value>
		///   <c>true</c> if the property should be cascaded; otherwise, <c>false</c>.
		/// </value>
		public bool ShouldCascade { get; set; } = true;
	}
}
