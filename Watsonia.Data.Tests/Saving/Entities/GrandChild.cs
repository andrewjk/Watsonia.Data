using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Saving.Entities
{
	public class GrandChild
	{
		public virtual Child Parent { get; set; }

		public virtual string Name { get; set; }
	}
}
