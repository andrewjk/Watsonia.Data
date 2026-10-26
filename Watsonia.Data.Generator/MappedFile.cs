using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.Generator
{
	class MappedFile
	{
		public string InputFile { get; set; }

		public string InputFolder { get; set; }

		public string OutputFolder { get; set; }

		public override string ToString()
		{
			return this.InputFile;
		}
	}
}
