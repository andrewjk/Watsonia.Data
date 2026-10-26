using System;
using System.Configuration;
using System.Linq;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Queries.Northwind
{
	internal sealed class NorthwindConfiguration : DatabaseConfiguration
	{
		public NorthwindConfiguration(string connectionString, string entityNamespace)
			: base(new SQLiteDataAccessProvider(), connectionString, entityNamespace)
		{
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

		public override string GetForeignKeyColumnName(Type tableType, Type foreignType)
		{
			return foreignType.Name + "ID";
		}

		public override string GetColumnName(System.Reflection.PropertyInfo property)
		{
			// The Order Details test table uses a string surrogate key called Id,
			// while the dynamic proxy names its generated primary key property OrderDetailID
			if (property.Name == "OrderDetailID")
			{
				return "Id";
			}
			return base.GetColumnName(property);
		}

		public override Type GetPrimaryKeyColumnType(Type type)
		{
			if (typeof(OrderDetail).IsAssignableFrom(type))
			{
				return typeof(string);
			}
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
