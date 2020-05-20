using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.DataAnnotations;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Reference
{
	public class AuthorValueBag : IValueBag
	{
		public long ID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public int? Age { get; set; }
		public double Rating { get; set; }
	}
}
