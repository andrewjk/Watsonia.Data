using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Cache.Entities
{
	public class Class
	{
		public virtual string Name { get; set; }

		public virtual Teacher Teacher { get; set; }

		public virtual IList<Student> Students { get; set; }
	}
}
