using System;
using System.Linq;

namespace Watsonia.Data.Tests.Northwind
{
	internal sealed class NorthwindConfiguration : DatabaseConfiguration
	{
		public NorthwindConfiguration(string connectionString, string entityNamespace)
			: base(connectionString, entityNamespace)
		{
			this.ProviderName = "Watsonia.Data.SqlServerCe";
		}

		public override string GetTableName(Type type)
		{
			if (type == typeof(OrderDetail))
			{
				return "Order Details";
			}
			else
			{
				return type.Name + "s";
			}
		}

		public override string GetPrimaryKeyColumnName(Type type)
		{
			return type.Name + "ID";
		}

		public override Type GetPrimaryKeyColumnType(Type type)
		{
			if (type == typeof(Customer))
			{
				return typeof(string);
			}
			else
			{
				return typeof(int);
			}
		}
	}
}
