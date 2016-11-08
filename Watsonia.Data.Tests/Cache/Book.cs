using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Cache
{
	public class Book
	{
		public virtual string Title { get; set; }

		public virtual Author Author { get; set; }

		public virtual IList<Chapter> Chapters { get; set; }
	}
}
