using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Editor.SourceParsing
{
	public interface IMatchFinder<TMatch> where TMatch : IMatch
	{		
		bool TryGetMatch(string source, out TMatch match);
	}
}
