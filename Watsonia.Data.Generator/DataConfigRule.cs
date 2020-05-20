using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Generator
{
	class DataConfigRule
	{
		public string Path { get; set; }

		public List<DataConfigMatch> ShouldMapType { get; set; }
	}
}
