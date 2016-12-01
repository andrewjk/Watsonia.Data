using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Reference
{
	public sealed class AuthorValueBag : IValueBag
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public int? Age { get; set; }
		public double Rating { get; set; }
	}
}
