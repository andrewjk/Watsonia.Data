using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data
{
	/// <summary>
	/// An interface for a dynamically created type that returns a new IDynamicProxy.
	/// </summary>
	public interface IDynamicProxyCreator
	{
		/// <summary>
		/// Creates an IDynamicProxy.
		/// </summary>
		/// <returns></returns>
		IDynamicProxy Create();
	}
}
