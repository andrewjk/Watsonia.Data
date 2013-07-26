using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.DocumentationModels
{
	public class Book : IValidatableObject
	{
		public bool IsNew
		{
			get;
			set;
		}

		public bool HasChanges
		{
			get;
			set;
		}

		[Required]
		public virtual string Title
		{
			get;
			set;
		}

		public virtual Author Author
		{
			get;
			set;
		}

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (this.Title == "Bad Book")
			{
				yield return new ValidationResult("Nope");
			}
		}

		public virtual void Undo()
		{
		}

		public virtual void Redo()
		{
		}
	}
}
