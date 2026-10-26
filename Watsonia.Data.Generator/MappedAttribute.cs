using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Generator
{
	class MappedAttribute
	{
		public string Name { get; set; }

		public List<string> Arguments { get; set; } = new List<string>();

		public override string ToString()
		{
			return $"{this.Name}({string.Join(", ", this.Arguments)})";
		}
	}
}
