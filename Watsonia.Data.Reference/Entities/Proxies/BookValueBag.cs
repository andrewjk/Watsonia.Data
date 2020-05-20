using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Reference
{
	public class BookValueBag : IValueBag
	{
		public long ID { get; set; }
		public string Title { get; set; }
		public decimal Price { get; set; }
		public long? AuthorID { get; set; }
	}
}
