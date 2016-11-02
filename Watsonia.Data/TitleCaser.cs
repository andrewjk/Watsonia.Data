using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// Contains functionality for converting text to title case.
	/// </summary>
	public static class TitleCaser
	{
		private static List<string> _lowerCaseWords = null;

		/// <summary>
		/// Gets a list of words that should always be output in lower case.
		/// </summary>
		public static List<string> LowerCaseWords
		{
			get
			{
				if (_lowerCaseWords == null)
				{
					// This is the array of words that should be output in lower case
					_lowerCaseWords = new List<string> {
						"a", "an", "and", "as", "at", "but", "by", "en", "for", "if", "in",
						"nor", "of", "on", "or", "the", "to", "v", "v.", "vs", "vs.", "via" };
				}
				return _lowerCaseWords;
			}
		}

		/// <summary>
		/// Converts the specified string to title case.
		/// </summary>
		/// <remarks>
		/// This functionality is based off John Gruber's title case function, described at http://daringfireball.net/2008/05/title_case.
		/// 
		/// It capitalizes each word in the specified string, with the following exceptions:
		/// <list type="bullet">
		///		<item><description>Some words are not capitalized, such as "for" and "the".</description></item>
		///		<item><description>Words with capitalized letters other than the first, words containing dots and words containing numbers are left unchanged.</description></item>
		///		<item><description>The first and last words are always capitalized.</description></item>
		///		<item><description>Unix style paths (i.e. starting with a forward slash) are changed to lower case.</description></item>
		///		<item><description>Words containing dashes or forward slashes are split and each part processed.</description></item>
		///	</list>
		///	</remarks>
		/// <param name="text">The string to convert to title case.</param>
		/// <param name="extraCases">Extra words or phrases with their intended case.</param>
		/// <returns>A string that consists of the text converted to title case.</returns>
		public static string ToTitleCase(string text, string[] extraCases = null)
		{
			if (text == null)
			{
				throw new ArgumentNullException("title");
			}

			// Split the title on white space
			string[] parts = text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			// Process each part of the title (recursively if necessary to split further)
			var b = new List<string>();
			for (int i = 0; i < parts.Length; i++)
			{
				char previousCharacter = (i > 0 ? parts[i - 1][parts[i - 1].Length - 1] : ' ');
				string processedPart = ProcessTitlePart(parts, i, previousCharacter);
				if (!string.IsNullOrWhiteSpace(processedPart))
				{
					b.Add(processedPart);
				}
			}

			// Re-join the title with spaces
			string result = string.Join(" ", b.ToArray());

			// Update the case of any extra phrases that were passed in
			if (extraCases != null)
			{
				foreach (string term in extraCases)
				{
					result = Regex.Replace(result, term, term, RegexOptions.IgnoreCase);
				}
			}

			return result;
		}

		private static string ProcessTitlePart(string[] parts, int index, char previousCharacter)
		{
			string result = parts[index];

			if (index > 0 &&
				index < (parts.Length - 1) &&
				previousCharacter != ':' &&
				previousCharacter != '-' &&
				previousCharacter != '–' &&
				TitleCaser.LowerCaseWords.Contains(parts[index].ToLower()) &&
				!(parts[index].Length > 1 && parts[index] == parts[index].ToUpper()))
			{
				// It's a small word, but not
				// * the first or last or after a colon, dash or em-dash
				// * longer than one character and all uppercase
				// and should be returned in lower-case
				result = parts[index].ToLower();
			}
			else if (parts[index].Length == 1)
			{
				// It's just one letter, capitalize it
				result = parts[index].ToUpper();
			}
			else if (parts[index].StartsWith("/"))
			{
				// It's a Unix style path and should be returned in lower-case
				result = parts[index].ToLower();
			}
			else if (Regex.IsMatch(parts[index], @"\w[A-Z]|\.\w|[0-9]"))
			{
				// It contains an upper-case letter (following another character), a dot (followed by
				// another character) or a number (anywhere) and should be returned as-is
				result = parts[index];
			}
			else if (parts[index].Contains("-"))
			{
				// It contains a hyphen and so each sub-part should be processed (e.g. Step-by-Step)
				result = ProcessTitleSubParts(parts[index], '-', previousCharacter);
			}
			else if (parts[index].Contains("/"))
			{
				// It contains a forward slash and so each sub-part should be processed (e.g. Could/Should)
				result = ProcessTitleSubParts(parts[index], '/', previousCharacter);
			}
			else
			{
				// The first letter should be capitalized (ignoring things like an opening parenthesis)
				for (int j = 0; j < parts[index].Length; j++)
				{
					if (char.IsLetter(parts[index][j]))
					{
						result = (j > 0 ? parts[index].Substring(0, j) : "") + parts[index][j].ToString().ToUpper() + parts[index].Substring(j + 1);
						break;
					}
				}
			}

			return result;
		}

		private static string ProcessTitleSubParts(string part, char separator, char previousCharacter)
		{
			string[] subParts = part.Split(new char[] { separator }, StringSplitOptions.RemoveEmptyEntries);
			for (int j = 0; j < subParts.Length; j++)
			{
				char subPreviousCharacter;
				if (j > 0)
				{
					subPreviousCharacter = subParts[j - 1][subParts[j - 1].Length - 1];
				}
				else
				{
					subPreviousCharacter = previousCharacter;
				}
				subParts[j] = ProcessTitlePart(subParts, j, subPreviousCharacter);
			}
			return string.Join(separator.ToString(), subParts);
		}
	}
}
