using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data.Tests.Database.Entities
{
	public class LoadingChild
	{
		public virtual int Value { get; set; }

		public virtual string Description { get; set; }

		[Cascade]
		public virtual List<LoadingSubChild> SubChildren { get; set; }
	}
}
