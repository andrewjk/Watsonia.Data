using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Reference
{
	public class AuthorCreator : IDynamicProxyCreator
	{
		public IDynamicProxy Create()
		{
			return new AuthorProxy();
		}
	}
}
