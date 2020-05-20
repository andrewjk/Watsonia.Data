﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Generator
{
	class MappedEntity
	{
		public string FileName { get; set; }

		public string OutputFolder { get; set; }

		public string Namespace { get; set; }

		public string Name { get; set; }

		public List<string> Usings { get; set; } = new List<string>();

		public List<MappedProperty> Properties { get; set; } = new List<MappedProperty>();

		public override string ToString()
		{
			return this.Name;
		}
	}
}
