using System;
using System.Collections.Generic;
using System.Text;

namespace Watsonia.Data.TestPerformance
{
	class ConsoleLinePart
	{
		public string Text { get; set; }
		public ConsoleColor Color { get; set; }

		public ConsoleLinePart(string text, ConsoleColor color = ConsoleColor.Gray)
		{
			this.Text = text;
			this.Color = color;
		}
	}
}
