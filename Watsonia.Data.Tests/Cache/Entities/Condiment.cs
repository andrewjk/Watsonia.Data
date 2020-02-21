using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Tests.Cache.Entities
{
	public class Condiment
	{
		public virtual int CondimentID { get; set; }

		public virtual string CondimentName { get; set; }

		public virtual decimal UnitPrice { get; set; }
	}
}
