using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Watsonia.Data.Query;

namespace Watsonia.Data
{
	public sealed class DatabaseQuery<T> : Query<T>
	{
		public DatabaseQuery(IQueryProvider provider, Type elementType)
			: base(provider)
		{
			base.ElementType = elementType;
		}

		public DatabaseQuery(IQueryProvider provider, Type staticType, Type elementType)
			: base(provider, staticType)
		{
			base.ElementType = elementType;
		}

		public DatabaseQuery(QueryProvider provider, Expression expression, Type elementType)
			: base(provider, expression)
		{
			base.ElementType = elementType;
		}

		public DatabaseQuery<T> Include(string path)
		{
			////_entityMappingEntity.IncludePaths.Add(path);
			return this;
		}
	}
}
