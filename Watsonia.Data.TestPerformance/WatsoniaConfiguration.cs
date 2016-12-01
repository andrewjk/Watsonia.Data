﻿using System;
using System.Linq;
using System.Reflection;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.TestPerformance
{
	internal sealed class WatsoniaConfiguration : DatabaseConfiguration
	{
		public WatsoniaConfiguration(string connectionString, string entityNamespace)
			: base(new SqlServerCeDataAccessProvider(), connectionString, entityNamespace)
		{
		}

		public override bool ShouldCacheType(Type type)
		{
			return false;
		}

		public override string GetTableName(Type type)
		{
			return base.GetTableName(type) + "s";
		}

		//// HACK: If we remove this override, we get two columns e.g. SportID and SportsID
		//// We should be connecting up related types and properties more intelligently
		//public override string GetForeignKeyColumnName(PropertyInfo property)
		//{
		//	return property.Name + "sID";
		//}

		//public override string GetForeignKeyColumnName(Type tableType, Type foreignType)
		//{
		//	return foreignType.Name + "sID";
		//}
	}
}
