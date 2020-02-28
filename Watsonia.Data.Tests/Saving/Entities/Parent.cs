using System;
using System.Collections.Generic;
using System.Linq;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.Saving.Entities
{
	public class Parent
	{
		public virtual string Name { get; set; }

		[Cascade]
		public virtual IList<Child> Children { get; set; }
	}
}
