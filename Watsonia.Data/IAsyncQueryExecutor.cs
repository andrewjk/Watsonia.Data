using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Remotion.Linq;

namespace Watsonia.Data
{
    public interface IAsyncQueryExecutor : IQueryExecutor
    {
		Task<IList<T>> ExecuteCollectionAsync<T>(QueryModel queryModel);

		Task<T> ExecuteScalarAsync<T>(QueryModel queryModel);

		Task<T> ExecuteSingleAsync<T>(QueryModel queryModel, bool returnDefaultWhenEmpty);

#if NET5_0
		IAsyncEnumerable<T> EnumerateCollectionAsync<T>(QueryModel queryModel);
#endif
	}
}
