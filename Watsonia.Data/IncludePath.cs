using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	internal sealed class IncludePath
	{
		public string Path { get; private set; }

		public PropertyInfo Property { get; set; }

		public IEnumerable ChildCollection { get; set; }

		public IncludePath(string path)
		{
			this.Path = path;
		}
	}
}
