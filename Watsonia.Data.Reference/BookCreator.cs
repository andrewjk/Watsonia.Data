using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Reference
{
	public class BookCreator : IDynamicProxyCreator
	{
		public IDynamicProxy Create()
		{
			return new BookProxy();
		}
	}
}
