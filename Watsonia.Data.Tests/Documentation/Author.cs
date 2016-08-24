using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Documentation
{
	public class Author
	{
		public virtual bool IsNew
		{
			get;
			set;
		}

		public virtual bool HasChanges
		{
			get;
			set;
		}

		public virtual bool IsValid
		{
			get
			{
				return true;
			}
		}

		public virtual IList<ValidationError> ValidationErrors
		{
			get
			{
				return new List<ValidationError>();
			}
		}

		public virtual void Undo()
		{
		}

		public virtual void Redo()
		{
		}

		// TODO: Raise this from the child class
		//public event EventHandler NameChanged;

		// Intercept this in the child class
		[Required]
		public virtual string FirstName
		{
			get;
			set;
		}

		// Call this from the child class
		protected virtual void OnFirstNameChanging(string value)
		{
		}

		// Call this from the child class
		protected virtual void OnFirstNameChanged()
		{
		}

		[StringLength(200)]
		[Required]
		public virtual string LastName
		{
			get;
			set;
		}

		protected virtual void OnLastNameChanging(string value)
		{
		}

		protected virtual void OnLastNameChanged()
		{
		}

		public string FullName
		{
			get
			{
				return string.Format("{0} {1}", this.FirstName, this.LastName);
			}
		}

		public virtual string Email
		{
			get;
			set;
		}

		public virtual DateTime? DateOfBirth
		{
			get;
			set;
		}

		public virtual int? Age
		{
			get;
			set;
		}

		public virtual int Rating
		{
			get;
			set;
		}

		public virtual IList<Book> Books
		{
			get;
			set;
		}
	}
}
