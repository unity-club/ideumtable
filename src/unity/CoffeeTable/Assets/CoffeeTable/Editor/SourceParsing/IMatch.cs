using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Editor.SourceParsing
{
	public interface IMatch
	{
		int LineNumber { get; set; }
		int Position { get; set; }
	}
}
