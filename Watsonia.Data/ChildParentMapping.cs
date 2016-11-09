using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	internal sealed class ChildParentMapping : Dictionary<Type, Stack<Type>>
	{
	}
}
