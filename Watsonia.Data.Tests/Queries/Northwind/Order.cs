using System;
using System.Collections.Generic;
using System.Linq;

namespace Watsonia.Data.Tests.Queries.Northwind
{
	public class Order
	{
		public virtual Customer Customer { get; set; }

		public virtual string CustomerID { get; set; }

		public virtual DateTime OrderDate { get; set; }

		public virtual int OrderID { get; set; }

		public virtual IList<OrderDetail> Details { get; set; }
	}
}
