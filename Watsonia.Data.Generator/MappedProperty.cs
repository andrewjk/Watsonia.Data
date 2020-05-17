using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Generator
{
	class MappedProperty
	{
		public string Name { get; set; }

		public string TypeName { get; set; }

		public bool IsRelatedCollection { get; set; }

		public bool IsRelatedItem { get; set; }

		public List<MappedAttribute> Attributes { get; set; } = new List<MappedAttribute>();
	}
}
