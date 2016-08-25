﻿using System;
using System.Linq;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.Tests.Northwind
{
	internal sealed class NorthwindConfiguration : DatabaseConfiguration
	{
		public NorthwindConfiguration(string connectionString, string entityNamespace)
			: base(new SqlServerCeDataAccessProvider(), connectionString, entityNamespace)
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
