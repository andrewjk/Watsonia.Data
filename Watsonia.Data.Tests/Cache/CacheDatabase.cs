using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.Tests.Cache
{
	public class CacheDatabase : Database
	{
		public CacheDatabase()
			: base(null, "", "Watsonia.Data.Tests.Cache")
		{
		}
	}
}
