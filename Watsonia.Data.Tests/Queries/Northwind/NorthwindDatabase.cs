using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.Data;

namespace Watsonia.Data.Tests.Queries.Northwind
{
	internal sealed class NorthwindDatabase : Watsonia.Data.Database
	{
		private const string ConnectionString = @"Data Source=Data/Northwind.sqlite";
		private const string EntityNamespace = "Watsonia.Data.Tests.Queries.Northwind";

		public NorthwindDatabase()
			: base(new NorthwindConfiguration(ConnectionString, EntityNamespace))
		{
		}

		public DatabaseQuery<Customer> Customers
		{
			get
			{
				return this.Query<Customer>();
			}
		}

		public DatabaseQuery<Order> Orders
		{
			get
			{
				return this.Query<Order>();
			}
		}

		public DatabaseQuery<OrderDetail> OrderDetails
		{
			get
			{
				return this.Query<OrderDetail>();
			}
		}
	}
}
