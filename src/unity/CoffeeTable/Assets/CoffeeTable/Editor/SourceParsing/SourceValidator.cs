using CoffeeTable.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace CoffeeTable.Editor.SourceParsing
{
	public static class SourceValidator
	{
		/// <summary>
		/// This method will ensure that the source files in the Assets folder all conform to the following specification
		/// when building to the Ideum Coffee Table.
		/// (*)	References to UnityEngine.Screen.fullScreen, UnityEngine.Screen.fullScreenMode, and UnityEngine.Screen.SetResolution
		///		are explicitly disallowed. Because the backend service manages the size and style of the windows, any call to these
		///		functions would break this system and cause unnecessary artifacting and interruptions to normal user experience.
		///		
		/// Note that this method should only be called to invalidate source *when building to the Ideum table*. Ordinary builds for
		/// the standalone players onto platforms such as Mac, Windows, or Android should not call this method as these standalone
		/// platforms do not warrant this specification.
		/// </summary>
		/// <returns>
		/// A boolean indicating whether or not the source was valid. True if the source files in the Assets folder
		/// matched the specification given above, otherwise false.
		/// </returns>
		public static bool ValidateSource()
		{
			// Get source files
			var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
			var scripts = assemblies
				.SelectMany(assembly => assembly.sourceFiles)
				.Where(file => !string.IsNullOrEmpty(file) && file.StartsWith("Assets", StringComparison.OrdinalIgnoreCase));

			if (!scripts.Any()) return true;

			StaticMatchFinder staticMatchFinder = new StaticMatchFinder();
			staticMatchFinder.Identifiers = new[]
			{
				new StaticMatchFinder.Identifier("UnityEngine.Screen", "fullScreen", "fullScreenMode", "SetResolution")
			};

			foreach (var file in scripts)
			{
				string source;
				try { source = File.ReadAllText(file); }
				catch (Exception e)
				{
					if (e is FileNotFoundException || e is NotSupportedException || e is SecurityException)
					{
						Log.BuildLog.LogWarn($"Failed to read source file at '{file}' while invalidating source files for compilation.");
						continue;
					}
					else throw;
				}

				if (string.IsNullOrWhiteSpace(source)) continue;

				source = SourceUtils.RemoveComments(source);
				source = SourceUtils.RemoveStrings(source);

				if (staticMatchFinder.TryGetMatch(source, out var match))
				{
					Log.BuildLog.LogError($"Failed to build project for table because an invalid identifier was found:\n" +
						$"'{match.Identifier}' at line {match.LineNumber}, position {match.Position} in file '{file}'.\n" +
						$"You are attempting to access the fullscreen and/or resolution settings provided by Unity in the {nameof(UnityEngine.Screen)} class. " +
						$"Accessing these settings may break the backend service running on the coffee table when your application is launched. " +
						$"Instead of using these identifiers, use the methods provided by the {nameof(Table)} class to interface with the backend service running on the coffee table.");
					return false;
				}
			}

			return true;
		}
	}
}
