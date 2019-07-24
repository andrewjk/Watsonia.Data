using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.TestPerformance
{
	class ConsoleLine
	{
		public List<ConsoleLinePart> Parts { get; } = new List<ConsoleLinePart>();

		public ConsoleLine(string line, ConsoleColor color = ConsoleColor.Gray)
		{
			Parts.Add(new ConsoleLinePart(line, color));
		}

		public ConsoleLine(params ConsoleLinePart[] parts)
		{
			Parts.AddRange(parts);
		}
	}
}
