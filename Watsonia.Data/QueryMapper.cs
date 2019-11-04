using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	public sealed class QueryMapper : DatabaseMapper
	{
		private readonly DatabaseConfiguration _configuration;

		public QueryMapper(DatabaseConfiguration configuration)
		{
			_configuration = configuration;
		}

		public override string GetSchemaName(Type type)
		{
			return _configuration.GetSchemaName(type);
		}

		public override string GetTableName(Type type)
		{
			return _configuration.GetTableName(type);
		}

		public override string GetFunctionName(Type type)
		{
			return _configuration.GetFunctionName(type);
		}

		public override string GetProcedureName(Type type)
		{
			return _configuration.GetProcedureName(type);
		}

		public override string GetColumnName(PropertyInfo property)
		{
			return _configuration.GetColumnName(property);
		}

		public override string GetPrimaryKeyColumnName(Type type)
		{
			return _configuration.GetPrimaryKeyColumnName(type);
		}

		public override bool IsRelatedItem(PropertyInfo property)
		{
			return _configuration.IsRelatedItem(property);
		}

		public override string GetForeignKeyColumnName(PropertyInfo property)
		{
			return _configuration.GetForeignKeyColumnName(property);
		}

		public override bool IsFunction(Type type)
		{
			return _configuration.IsFunction(type);
		}

		public override bool IsProcedure(Type type)
		{
			return _configuration.IsProcedure(type);
		}

		public override bool ShouldMapType(Type type)
		{
			return _configuration.ShouldMapType(type);
		}
	}
}
