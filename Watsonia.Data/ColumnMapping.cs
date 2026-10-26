using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data
{
	public class ColumnMapping
	{
		public string Name { get; set; }

		public string TypeName { get; set; }

		public bool IsRelatedCollection { get; set; }

		public string CollectionTypeName { get; set; }

		public bool IsRelatedItem { get; set; }

		public override string ToString()
		{
			return $"{TypeName} {Name}";
		}
	}
}
