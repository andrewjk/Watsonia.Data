using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQToolkit.Data;
using System.Linq.Expressions;
using System.Collections;
using Watsonia.Data.Linq;
using IQToolkit;

namespace Watsonia.Data
{
	public sealed class DatabaseQuery<T> : IQueryable<T>
	{
		private readonly IQueryable<T> _innerQueryable;
		private readonly EntityMappingEntity _entityMappingEntity;

		internal DatabaseQuery(IQueryable<T> innerQueryable, EntityMappingEntity entityMappingEntity)
		{
			_innerQueryable = innerQueryable;
			_entityMappingEntity = entityMappingEntity;
		}

		public DatabaseQuery<T> Include(string path)
		{
			_entityMappingEntity.IncludePaths.Add(path);
			return this;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _innerQueryable.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_innerQueryable).GetEnumerator();
		}

		public Type ElementType
		{
			get
			{
				return _innerQueryable.ElementType;
			}
		}

		public Expression Expression
		{
			get
			{
				return _innerQueryable.Expression;
			}
		}

		public IQueryProvider Provider
		{
			get
			{
				return _innerQueryable.Provider;
			}
		}
	}
}
