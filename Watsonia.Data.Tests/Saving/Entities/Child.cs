using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.Saving.Entities
{
	public class Child
	{
		public virtual string Name { get; set; }

		public virtual Parent Parent { get; set; }

		public virtual List<GrandChild> GrandChildren { get; set; }
	}
}
