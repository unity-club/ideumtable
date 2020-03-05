using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

/*
 * //////////////////////////////////////////////////////////////////////
 * 
 * Taken from: http://www.hardcodet.net/2008/03/wpf-file-selection-control
 * and refactored by Unity Club, 2020
 * 
 * //////////////////////////////////////////////////////////////////////
 */
namespace CoffeeTableLauncher.Controls
{
	/// <summary>
	/// Interaction logic for FileSelector.xaml
	/// </summary>
	public partial class FileSelector : UserControl
	{
		public static readonly DependencyProperty FileFilterProperty;
		public static readonly DependencyProperty FileNameProperty;
		public static readonly DependencyProperty TitleProperty;

		public string FileFilter
		{
			get { return (string)GetValue(FileFilterProperty); }
			set { SetValue(FileFilterProperty, value); }
		}

		public string FileName
		{
			get { return (string)GetValue(FileNameProperty); }
			set { SetValue(FileNameProperty, value); }
		}

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		static FileSelector()
		{
			/* Register dependency properties */

			/* File Name Property */
			FrameworkPropertyMetadata md = new FrameworkPropertyMetadata("", (d, e) => {
				FileSelector owner = (FileSelector)d;
				owner.UpdateFileNameDisplay();
			});
			FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(FileSelector), md);

			/* File Filter Property */
			md = new FrameworkPropertyMetadata(null, (PropertyChangedCallback) null);
			FileFilterProperty = DependencyProperty.Register("FileFilter", typeof(string), typeof(FileSelector), md);

			/* Dialog Title Property */
			md = new FrameworkPropertyMetadata("Choose a file...", null);
			TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FileSelector), md);
		}


		public FileSelector()
		{
			InitializeComponent();
		}

		private void UpdateFileNameDisplay()
		{
			string fileName = FileName;

			if (String.IsNullOrEmpty(fileName))
			{
				txtFileName.Text = String.Empty;
				return;
			}

			txtFileName.Text = fileName;
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			FileDialog dlg = new OpenFileDialog();

			dlg.Filter = FileFilter;
			dlg.Title = Title;
			bool? result = dlg.ShowDialog();
			if (result == true) FileName = dlg.FileName;
		}
	}
}
