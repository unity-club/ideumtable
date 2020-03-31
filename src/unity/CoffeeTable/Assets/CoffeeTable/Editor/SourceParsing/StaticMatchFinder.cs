using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoffeeTable.Editor.SourceParsing
{
	public class StaticMatchFinder : IMatchFinder<StaticMatchFinder.StaticMatch>
	{
		public Identifier[] Identifiers { get; set; }

		public class StaticMatch : IMatch
		{
			public string Identifier { get; set; }
			public int LineNumber { get; set; }
			public int Position { get; set; }
		}

		public class Identifier
		{
			public string QualifiedClassName { get; private set; }
			public string[] Tokens { get; private set; }

			public string ClassName { get; private set; }
			public string[] Hierarchy { get; private set; }

			public Regex[] FullyQualifiedRegexes { get; private set; }
			public (Regex Namespace, Regex QualifiedToken)[] PartiallyQualifiedRegex { get; private set; }

			public Identifier(string qualifiedClassName, params string[] tokens)
			{
				if (string.IsNullOrWhiteSpace(qualifiedClassName)) throw new ArgumentException();
				if (qualifiedClassName.IndexOf(' ') >= 0) throw new ArgumentException();

				QualifiedClassName = qualifiedClassName.Trim();
				Tokens = tokens.Select(t =>
				{
					if (string.IsNullOrWhiteSpace(t)) throw new ArgumentException();
					if (t.IndexOf(' ') >= 0) throw new ArgumentException();
					var trimmed = t.Trim();
					if (trimmed.StartsWith(".") || trimmed.EndsWith(".")) throw new ArgumentException();
					return trimmed;
				}).ToArray();

				if (qualifiedClassName.StartsWith(".") || qualifiedClassName.EndsWith(".")) throw new ArgumentException();

				Hierarchy = qualifiedClassName.Split(new[] { '.' }, StringSplitOptions.None);
				if (!Hierarchy.Any() || Hierarchy.Any(i => string.IsNullOrWhiteSpace(i))) throw new ArgumentException();

				// Update regexes
				FullyQualifiedRegexes = GetFullyQualifiedRegexes();
				PartiallyQualifiedRegex = GetPartialTokenRegexes();
			}

			public Regex GetUsingRegex() => new Regex($@"\busing +(\w+) *= *{Regex.Escape(QualifiedClassName)}\b");
			private string[] GetFullyQualifiedTokens() => Tokens.Select(i => $"{QualifiedClassName}.{i}").ToArray();
			private Regex[] GetFullyQualifiedRegexes() => GetFullyQualifiedTokens()
				.Select(t => new Regex($@"\b{Regex.Escape(t)}\b")).ToArray();

			private (Regex Namespace, Regex QualifiedToken)[] GetPartialTokenRegexes()
			{
				var regexPairs = new List<(Regex, Regex)>();

				for (int i = 0; i < Hierarchy.Length - 1; i++)
				{
					string usingNs = string.Empty;
					string qualified = string.Empty;

					for (int j = 0; j <= i; j++)
					{
						usingNs += Hierarchy[j];
						if (j != i) usingNs += '.';
					}

					for (int j = i + 1; j < Hierarchy.Length; j++)
					{
						qualified += Hierarchy[j];
						if (j != Hierarchy.Length - 1) qualified += '.';
					}

					Regex usingRegex = new Regex($@"\busing +{Regex.Escape(usingNs)}\b");

					foreach (string token in Tokens)
						regexPairs.Add((usingRegex, new Regex($@"\b{Regex.Escape(qualified)}\.{Regex.Escape(token)}\b")));
				}

				return regexPairs.ToArray();
			}
		}

		private IEnumerable<Regex> GetUsingsRegexes(Identifier i, string source)
		{
			List<Regex> usingRegexes = new List<Regex>();
			var matches = i.GetUsingRegex().Matches(source);

			foreach (Match m in matches)
			{
				if (m.Groups[1].Captures.Count == 0) continue;
				foreach (string token in i.Tokens)
					usingRegexes.Add(new Regex($@"\b{m.Groups[1].Captures[0].Value}\.{Regex.Escape(token)}"));
			}
			return usingRegexes;
		}

		public bool TryGetMatch(string source, out StaticMatch matchedIdentifier)
		{
			matchedIdentifier = null;

			// Reduce into line array
			if (string.IsNullOrWhiteSpace(source)) return false;
			string[] lines = source.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

			// Go through identifiers
			foreach (var identifier in Identifiers)
			{
				var usingRegexes = GetUsingsRegexes(identifier, source);

				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];
					int lineNumber = i + 1;

					// Start with fully qualified usages of the class and token
					foreach (var reg in identifier.FullyQualifiedRegexes)
					{
						var match = reg.Match(line);
						if (match.Success)
						{
							matchedIdentifier = new StaticMatch()
							{
								Identifier = match.Value,
								LineNumber = lineNumber,
								Position = match.Index
							};
							return true;
						}
					}

					// Go on to partially qualified usages of the class and token
					foreach (var regPair in identifier.PartiallyQualifiedRegex)
					{
						var partiallyQualifiedMatch = regPair.QualifiedToken.Match(line);
						if (partiallyQualifiedMatch.Success)
						{
							// ensure that they are using the rest of the namespace
							if (regPair.Namespace.IsMatch(source))
							{
								matchedIdentifier = new StaticMatch()
								{
									Identifier = partiallyQualifiedMatch.Value,
									LineNumber = lineNumber,
									Position = partiallyQualifiedMatch.Index
								};
								return true;
							}
						}
					}

					// Go on to using <s> = FullyQualifiedStaticClassName notation
					foreach (var reg in usingRegexes)
					{
						var m = reg.Match(line);
						if (m.Success)
						{
							matchedIdentifier = new StaticMatch
							{
								Identifier = m.Value,
								LineNumber = lineNumber,
								Position = m.Index
							};
							return true;
						}
					}
				}
			}
			return false;
		}

	}
}
