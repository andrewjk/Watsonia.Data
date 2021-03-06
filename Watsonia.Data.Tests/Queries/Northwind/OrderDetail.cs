﻿using System;
using System.Linq;

namespace Watsonia.Data.Tests.Queries.Northwind
{
	public class OrderDetail
	{
		public virtual int OrderID { get; set; }

		public virtual Product Product { get; set; }

		public virtual int ProductID { get; set; }
	}
}
