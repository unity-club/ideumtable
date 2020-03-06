using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CoffeeTableLauncher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{

		public MainWindow()
		{
			InitializeComponent();
			ProgressBar.Visibility = Visibility.Hidden;
			ProgressBar.Value = 100;

			SetupTileGrid();
		}

		List<TileData> tiles = new List<TileData>();

		/* Test method */
		private void SetupTileGrid()
		{
			for (int i = 0; i < 25; i++)
				tiles.Add(new TileData { Color = new SolidColorBrush(Colors.Cyan), Id = "1", Title="TestTitle", Description="Description is here." });

			ItemList.ItemsSource = tiles;
		}

		private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
		}
	}

	public class TileData
	{
		public Brush Color { get; set; }
		public string Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
	}
}
