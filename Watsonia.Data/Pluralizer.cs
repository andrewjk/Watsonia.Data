using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// Contains functionality for pluralizing text.
	/// </summary>
	public static class Pluralizer
	{
		private static Dictionary<string, string> _exceptions = null;

		/// <summary>
		/// Gets a dictionary of exceptions that should be pluralized in a special way where the key
		/// is the text to be pluralized and the value is the pluralization result.
		/// </summary>
		/// <value>
		/// The exceptions.
		/// </value>
		public static Dictionary<string, string> Exceptions
		{
			get
			{
				if (_exceptions == null)
				{
					// Create the default list of exceptions that should be checked first
					// This is very much not an exhaustive list!
					_exceptions = new Dictionary<string, string>() {
						{ "man", "men" },
						{ "woman", "women" },
						{ "child", "children" },
						{ "tooth", "teeth" },
						{ "foot", "feet" },
						{ "mouse", "mice" },
						{ "belief", "beliefs" } };
				}
				return _exceptions;
			}
		}

		/// <summary>
		/// Attempts to pluralize the specified text according to the rules of the English language.
		/// </summary>
		/// <remarks>
		/// This function attempts to pluralize as many words as practical by following these rules:
		/// <list type="bullet">
		///		<item><description>Words that don't follow any rules (e.g. "mouse" becomes "mice") are returned from a dictionary.</description></item>
		///		<item><description>Words that end with "y" (but not with a vowel preceding the y) are pluralized by replacing the "y" with "ies".</description></item>
		///		<item><description>Words that end with "us", "ss", "x", "ch" or "sh" are pluralized by adding "es" to the end of the text.</description></item>
		///		<item><description>Words that end with "f" or "fe" are pluralized by replacing the "f(e)" with "ves".</description></item>
		///	</list>
		/// </remarks>
		/// <param name="text">The text to pluralize.</param>
		/// <param name="number">If number is 1, the text is not pluralized; otherwise, the text is pluralized.</param>
		/// <returns>A string that consists of the text in its pluralized form.</returns>
		public static string Pluralize(string text, int number = 2)
		{
			if (number == 1)
			{
				return text;
			}
			else
			{
				if (Pluralizer.Exceptions.ContainsKey(text.ToLowerInvariant()))
				{
					return Pluralizer.Exceptions[text.ToLowerInvariant()];
				}
				else if (text.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("ay", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("ey", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("iy", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("oy", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("uy", StringComparison.OrdinalIgnoreCase))
				{
					return text.Substring(0, text.Length - 1) + "ies";
				}
				else if (text.EndsWith("us", StringComparison.InvariantCultureIgnoreCase))
				{
					// http://en.wikipedia.org/wiki/Plural_form_of_words_ending_in_-us
					return text + "es";
				}
				else if (text.EndsWith("ss", StringComparison.InvariantCultureIgnoreCase))
				{
					return text + "es";
				}
				else if (text.EndsWith("s", StringComparison.InvariantCultureIgnoreCase))
				{
					return text;
				}
				else if (text.EndsWith("x", StringComparison.InvariantCultureIgnoreCase) ||
					text.EndsWith("ch", StringComparison.InvariantCultureIgnoreCase) ||
					text.EndsWith("sh", StringComparison.InvariantCultureIgnoreCase))
				{
					return text + "es";
				}
				else if (text.EndsWith("f", StringComparison.InvariantCultureIgnoreCase) && text.Length > 1)
				{
					return text.Substring(0, text.Length - 1) + "ves";
				}
				else if (text.EndsWith("fe", StringComparison.InvariantCultureIgnoreCase) && text.Length > 2)
				{
					return text.Substring(0, text.Length - 2) + "ves";
				}
				else
				{
					return text + "s";
				}
			}
		}
	}
}
