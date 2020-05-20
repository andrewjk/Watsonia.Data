using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Reference
{
	public class Author
	{
		public virtual string FirstName { get; set; }

		public virtual string LastName { get; set; }

		public string FullName
		{
			get
			{
				return $"{this.FirstName} {this.LastName}";
			}
		}

		public virtual string Email { get; set; }

		[DefaultDateTimeValue(1, 1, 1995)]
		public virtual DateTime? DateOfBirth { get; set; }

		[DefaultValue(18)]
		public virtual int? Age { get; set; }

		[DefaultValue(5)]
		public virtual double Rating { get; set; }

		public virtual IList<Book> Books { get; set; }
	}
}
