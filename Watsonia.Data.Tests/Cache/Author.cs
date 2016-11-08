using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Cache
{
	public class Author
	{
		public virtual string FirstName { get; set; }

		public virtual string LastName { get; set; }
		
		public string FullName
		{
			get
			{
				return string.Format("{0} {1}", this.FirstName, this.LastName);
			}
		}

		public virtual string Email { get; set; }

		public virtual DateTime? DateOfBirth { get; set; }

		public virtual int? Age { get; set; }

		public virtual int Rating { get; set; }

		public virtual IList<Book> Books { get; set; }
	}
}
