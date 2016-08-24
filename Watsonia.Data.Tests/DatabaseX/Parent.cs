using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Tests.DatabaseX
{
	public class Parent
	{
		public virtual string Name
		{
			get;
			set;
		}

		[Cascade]
		public virtual IList<Child> Children
		{
			get;
			set;
		}
	}
}