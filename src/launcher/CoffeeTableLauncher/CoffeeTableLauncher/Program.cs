﻿using System;

namespace CoffeeTableLauncher
{
	public class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
