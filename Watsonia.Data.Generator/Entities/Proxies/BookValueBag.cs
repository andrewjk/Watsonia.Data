using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data;
using Watsonia.Data.EventArgs;

namespace Watsonia.Data.Generator.Proxies
{
	public class BookValueBag : IValueBag
	{
		public long ID { get; set; }
		public string Title { get; set; }
		public Author Author { get; set; }
		public decimal Price { get; set; }
	}
}
