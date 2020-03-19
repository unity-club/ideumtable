using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeTable.Module.Window
{
	public static class NativeMethods
	{
		private const string WindowFunctionsDll = "CoffeeTable.Windows.dll";

		[DllImport(WindowFunctionsDll)]
		internal static extern void GetScreenResolution(out int width, out int height);

		[DllImport(WindowFunctionsDll)]
		internal static extern bool GetWindowCoords(IntPtr handle, out int x, out int y, out int width, out int height);

		[DllImport(WindowFunctionsDll)]
		internal static extern void StyleWindow(IntPtr handle, bool flag);

		[DllImport(WindowFunctionsDll)]
		internal static extern void SetWindowCoords(IntPtr handle, int x, int y, int cx, int cy);


	}
}
