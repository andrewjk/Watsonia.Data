using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	internal interface IEntityTable : IQueryable/*, IUpdatable*/
	{
		new QueryProvider Provider { get; }
		string TableId { get; }
		object GetById(object id);
		int Insert(object instance);
		int Update(object instance);
		int Delete(object instance);
		int InsertOrUpdate(object instance);
	}

	internal interface IEntityTable<T> : IQueryable<T>, IEntityTable/*, IUpdatable<T>*/
	{
		new T GetById(object id);
		int Insert(T instance);
		int Update(T instance);
		int Delete(T instance);
		int InsertOrUpdate(T instance);
	}
}
