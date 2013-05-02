using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests
{
	[TestClass]
	public class TitleCaseTests
	{
		[TestMethod]
		public void TestTitleCases()
		{
			// From https://github.com/ap/titlecase/blob/master/test.pl
			Assert.AreEqual("For Step-by-Step Directions Email someone@gmail.com", TitleCaser.ToTitleCase("For step-by-step directions email someone@gmail.com"));
			Assert.AreEqual("2lmc Spool: 'Gruber on OmniFocus and Vapo(u)rware'", TitleCaser.ToTitleCase("2lmc Spool: 'Gruber on OmniFocus and Vapo(u)rware'"));
			Assert.AreEqual("Have You Read “The Lottery”?", TitleCaser.ToTitleCase("Have you read “The Lottery”?"));
			Assert.AreEqual("Your Hair[cut] Looks (Nice)", TitleCaser.ToTitleCase("your hair[cut] looks (nice)"));
			Assert.AreEqual("People Probably Won't Put http://foo.com/bar/ in Titles", TitleCaser.ToTitleCase("People probably won't put http://foo.com/bar/ in titles"));
			// NOTE: Replaced the original non-breaking hyphens with regular hyphens
			Assert.AreEqual("Scott Moritz and TheStreet.com’s Million iPhone La-La Land", TitleCaser.ToTitleCase("Scott Moritz and TheStreet.com’s million iPhone la-la land"));
			Assert.AreEqual("BlackBerry vs. iPhone", TitleCaser.ToTitleCase("BlackBerry vs. iPhone"));
			Assert.AreEqual("Notes and Observations Regarding Apple’s Announcements From ‘The Beat Goes On’ Special Event", TitleCaser.ToTitleCase("Notes and observations regarding Apple’s announcements from ‘The Beat Goes On’ special event"));
			Assert.AreEqual("Read markdown_rules.txt to Find Out How _Underscores Around Words_ Will Be Interpreted", TitleCaser.ToTitleCase("Read markdown_rules.txt to find out how _underscores around words_ will be interpreted"));
			Assert.AreEqual("Q&A With Steve Jobs: 'That's What Happens in Technology'", TitleCaser.ToTitleCase("Q&A with Steve Jobs: 'That's what happens in technology'"));
			Assert.AreEqual("What Is AT&T's Problem?", TitleCaser.ToTitleCase("What is AT&T's problem?"));
			Assert.AreEqual("Apple Deal With AT&T Falls Through", TitleCaser.ToTitleCase("Apple deal with AT&T falls through"));
			Assert.AreEqual("This v That", TitleCaser.ToTitleCase("this v that"));
			Assert.AreEqual("This vs That", TitleCaser.ToTitleCase("this vs that"));
			Assert.AreEqual("This v. That", TitleCaser.ToTitleCase("this v. that"));
			Assert.AreEqual("This vs. That", TitleCaser.ToTitleCase("this vs. that"));
			Assert.AreEqual("The SEC's Apple Probe: What You Need to Know", TitleCaser.ToTitleCase("The SEC's Apple probe: what you need to know"));
			Assert.AreEqual("'By the Way, Small Word at the Start but Within Quotes.'", TitleCaser.ToTitleCase("'by the way, small word at the start but within quotes.'"));
			Assert.AreEqual("Small Word at End Is Nothing to Be Afraid Of", TitleCaser.ToTitleCase("Small word at end is nothing to be afraid of"));
			Assert.AreEqual("Starting Sub-Phrase With a Small Word: A Trick, Perhaps?", TitleCaser.ToTitleCase("Starting sub-phrase with a small word: a trick, perhaps?"));
			Assert.AreEqual("Sub-Phrase With a Small Word in Quotes: 'A Trick, Perhaps?'", TitleCaser.ToTitleCase("Sub-phrase with a small word in quotes: 'a trick, perhaps?'"));
			Assert.AreEqual("Sub-Phrase With a Small Word in Quotes: \"A Trick, Perhaps?\"", TitleCaser.ToTitleCase("Sub-phrase with a small word in quotes: \"a trick, perhaps?\""));
			Assert.AreEqual("\"Nothing to Be Afraid Of?\"", TitleCaser.ToTitleCase("\"Nothing to Be Afraid of?\""));
			Assert.AreEqual("A Thing", TitleCaser.ToTitleCase("a thing"));
			Assert.AreEqual("Dr. Strangelove (Or: How I Learned to Stop Worrying and Love the Bomb)", TitleCaser.ToTitleCase("Dr. Strangelove (or: how I Learned to Stop Worrying and Love the Bomb)"));
			Assert.AreEqual("This Is Trimming", TitleCaser.ToTitleCase("  this is trimming"));
			Assert.AreEqual("This Is Trimming", TitleCaser.ToTitleCase("this is trimming  "));
			Assert.AreEqual("This Is Trimming", TitleCaser.ToTitleCase("  this is trimming  "));
			// NOTE: I can't find a way to do this that would preserve things that should stay in upper-case (e.g. acronyms)
			// Assert.AreEqual("If It’s All Caps, Fix It", TitleCaser.ToTitleCase("IF IT’S ALL CAPS, FIX IT"));
			Assert.AreEqual("What Could/Would/Should Be Done About Slashes?", TitleCaser.ToTitleCase("What could/would/should be done about slashes?"));
			Assert.AreEqual("Never Touch Paths Like /var/run Before/After /boot", TitleCaser.ToTitleCase("Never touch paths like /var/run before/after /boot"));
			Assert.AreEqual("If There's a Dash in the Middle of a Sentence - Leave It Be", TitleCaser.ToTitleCase("If there's a dash in the middle of a sentence - leave it be"));

			// From https://github.com/gouch/to-title-case
			Assert.AreEqual("A for by Of", TitleCaser.ToTitleCase("A For By Of"));
			Assert.AreEqual("Follow Step-by-Step Instructions", TitleCaser.ToTitleCase("follow step-by-step instructions"));
			Assert.AreEqual("This Sub-Phrase Is Nice", TitleCaser.ToTitleCase("this sub-phrase is nice"));
			Assert.AreEqual("Catchy Title: A Subtitle", TitleCaser.ToTitleCase("catchy title: a subtitle"));
			Assert.AreEqual("Catchy Title: \"A Quoted Subtitle\"", TitleCaser.ToTitleCase("catchy title: \"a quoted subtitle\""));
			Assert.AreEqual("Catchy Title: “‘A Twice Quoted Subtitle’”", TitleCaser.ToTitleCase("catchy title: “‘a twice quoted subtitle’”"));
			Assert.AreEqual("\"A Title Inside Double Quotes\"", TitleCaser.ToTitleCase("\"a title inside double quotes\""));
			Assert.AreEqual("All Words Capitalized", TitleCaser.ToTitleCase("all words capitalized"));
			Assert.AreEqual("Small Words the Lowercase", TitleCaser.ToTitleCase("small words the lowercase"));
			Assert.AreEqual("A Small Word Starts", TitleCaser.ToTitleCase("a small word starts"));
			Assert.AreEqual("Do Questions Work?", TitleCaser.ToTitleCase("do questions work?"));
			Assert.AreEqual("Multiple Sentences. More Than One.", TitleCaser.ToTitleCase("multiple sentences. more than one."));
			Assert.AreEqual("Ends With Small Word Of", TitleCaser.ToTitleCase("Ends with small word of"));
			Assert.AreEqual("\"Title Inside Double Quotes\"", TitleCaser.ToTitleCase("\"title inside double quotes\""));
			Assert.AreEqual("Double Quoted \"Inner\" Word", TitleCaser.ToTitleCase("double quoted \"inner\" word"));
			Assert.AreEqual("Single Quoted 'Inner' Word", TitleCaser.ToTitleCase("single quoted 'inner' word"));
			Assert.AreEqual("Fancy Double Quoted “Inner” Word", TitleCaser.ToTitleCase("fancy double quoted “inner” word"));
			Assert.AreEqual("Fancy Single Quoted ‘Inner’ Word", TitleCaser.ToTitleCase("fancy single quoted ‘inner’ word"));
			Assert.AreEqual("This vs. That", TitleCaser.ToTitleCase("this vs. that"));
			Assert.AreEqual("This vs That", TitleCaser.ToTitleCase("this vs that"));
			Assert.AreEqual("This v. That", TitleCaser.ToTitleCase("this v. that"));
			Assert.AreEqual("This v That", TitleCaser.ToTitleCase("this v that"));
			Assert.AreEqual("Address email@example.com Titles", TitleCaser.ToTitleCase("address email@example.com titles"));
			Assert.AreEqual("Pass camelCase Through", TitleCaser.ToTitleCase("pass camelCase through"));
			Assert.AreEqual("Don't Break", TitleCaser.ToTitleCase("don't break"));
			Assert.AreEqual("Catchy Title: Substance Subtitle", TitleCaser.ToTitleCase("catchy title: substance subtitle"));
			Assert.AreEqual("We Keep NASA Capitalized", TitleCaser.ToTitleCase("we keep NASA capitalized"));
			Assert.AreEqual("Leave Q&A Unscathed", TitleCaser.ToTitleCase("leave Q&A unscathed"));
			Assert.AreEqual("Scott Moritz and TheStreet.com’s Million iPhone La-La Land", TitleCaser.ToTitleCase("Scott Moritz and TheStreet.com’s million iPhone la-la land"));
			Assert.AreEqual("You Have a http://example.com/foo/ Title", TitleCaser.ToTitleCase("you have a http://example.com/foo/ title"));
			Assert.AreEqual("Your Hair[cut] Looks (Nice)", TitleCaser.ToTitleCase("your hair[cut] looks (nice)"));
			Assert.AreEqual("Keep That Colo(u)r", TitleCaser.ToTitleCase("keep that colo(u)r"));
			Assert.AreEqual("Have You Read “The Lottery”?", TitleCaser.ToTitleCase("have you read “The Lottery”?"));
			Assert.AreEqual("Read markdown_rules.txt to Find Out How _Underscores Around Words_ Will Be Interpreted", TitleCaser.ToTitleCase("Read markdown_rules.txt to find out how _underscores around words_ will be interpreted"));
			Assert.AreEqual("Read markdown_rules.txt to Find Out How *Asterisks Around Words* Will Be Interpreted", TitleCaser.ToTitleCase("Read markdown_rules.txt to find out how *asterisks around words* will be interpreted"));
			Assert.AreEqual("Notes and Observations Regarding Apple’s Announcements From ‘The Beat Goes On’ Special Event", TitleCaser.ToTitleCase("Notes and observations regarding Apple’s announcements from ‘The Beat Goes On’ special event"));
		}
	}
}
