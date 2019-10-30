using System;

namespace Watsonia.Data.DataAnnotations
{
	/// <summary>
	/// An attribute to place on entity properties when their columns should be renamed in the database.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class RenameAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the old name of the field.
		/// </summary>
		/// <value>
		/// The old name.
		/// </value>
		public string OldName { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RenameAttribute"/> class.
		/// </summary>
		/// <param name="oldName">The old name.</param>
		public RenameAttribute(string oldName)
		{
			this.OldName = oldName;
		}
	}
}
