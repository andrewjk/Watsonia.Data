using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Reference
{
	public class Book
	{
		public virtual long ID { get; set; }

		public virtual string Title { get; set; }

		public virtual Author Author { get; set; }

		[DefaultValue(10)]
		public virtual decimal Price { get; set; }
	}
}
