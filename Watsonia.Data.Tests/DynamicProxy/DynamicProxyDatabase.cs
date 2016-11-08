using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class DynamicProxyDatabase : Database
	{
		public DynamicProxyDatabase()
			: base(null, "", "Watsonia.Data.Tests.DynamicProxy")
		{
		}
	}
}
