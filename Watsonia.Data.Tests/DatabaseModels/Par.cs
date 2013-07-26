using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Tests.DatabaseModels
{
	public class Par
	{
		public virtual string Name
		{
			get;
			set;
		}

		[Cascade]
		public virtual IList<Chil> Chils
		{
			get;
			set;
		}
	}
}