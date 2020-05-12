using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// This is just for getting the include paths out of DatabaseQuery when building the execution plan.
	/// </summary>
	internal interface IDatabaseQuery
	{
		List<string> IncludePaths { get; }
	}
}
