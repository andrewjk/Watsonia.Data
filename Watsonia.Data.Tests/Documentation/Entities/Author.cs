﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Documentation.Entities
{
	public class Author
	{
		public virtual long ID { get; set; }

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
		public virtual string FirstName { get; set; }

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
		public virtual string LastName { get; set; }

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
				return $"{this.FirstName} {this.LastName}";
			}
		}

		public virtual string Email { get; set; }

		public virtual DateTime? DateOfBirth { get; set; }

		public virtual int? Age { get; set; }

		public virtual int Rating { get; set; }

		public virtual IList<Book> Books { get; set; }
	}
}
