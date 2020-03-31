using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoffeeTable.Editor.SourceParsing
{
	public static class SourceUtils
	{
		private const string blockComments = @"/\*(.*?)\*/";
		private const string lineComments = @"//(.*?)\r?\n";
		private const string strings = @"""((\\[^\n]|[^""\n])*)""";
		private const string verbatimStrings = @"@(""[^""]*"")+";

		//
		// Taken from: https://stackoverflow.com/a/3524689/10149816
		// Removes comments from a string of source code
		//
		public static string RemoveComments (string source)
		{
			return Regex.Replace(source,
				blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
				me => {
					if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
						return me.Value.StartsWith("//") ? Environment.NewLine : "";
					// Keep the literal strings
					return me.Value;
				},
				RegexOptions.Singleline);
		}

		public static string RemoveStrings (string source)
		{
			return Regex.Replace(source, @"\""(.*?)\""", string.Empty, RegexOptions.Singleline);
		}
	}
}
