using System;
using System.Collections.Generic;
using System.Linq;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.Database.Entities
{
	public class LoadingParent
	{
		public virtual string Name { get; set; }

		[Cascade]
		public virtual IList<LoadingChild> Children { get; set; }
	}
}
