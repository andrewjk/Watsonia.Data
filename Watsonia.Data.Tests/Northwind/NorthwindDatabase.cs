using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Tests.Northwind
{
	internal sealed class NorthwindDatabase : Database
	{
		private const string ConnectionString = @"Data Source=Data\Northwind.sdf;Persist Security Info=False";
		private const string EntityNamespace = "Watsonia.Data.Tests.Northwind";

		public NorthwindDatabase()
			: base(new NorthwindConfiguration(ConnectionString, EntityNamespace))
		{
		}

		public IQueryable<Customer> Customers
		{
			get
			{
				return this.Query<Customer>();
			}
		}

		public IQueryable<Order> Orders
		{
			get
			{
				return this.Query<Order>();
			}
		}

		public IQueryable<OrderDetail> OrderDetails
		{
			get
			{
				return this.Query<OrderDetail>();
			}
		}
	}
}
