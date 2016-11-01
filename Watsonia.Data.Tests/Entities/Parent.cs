using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Tests.Entities
{
	public class Parent
	{
		public virtual string Name { get; set; }

		[Cascade]
		public virtual IList<Child> Children { get; set; }
	}
}