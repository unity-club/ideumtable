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
		public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register("FileFilter", typeof(string), typeof(FileSelector), new FrameworkPropertyMetadata(null, (PropertyChangedCallback)null));
		public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(FileSelector), new FrameworkPropertyMetadata("",
			(d, e) => {
				FileSelector owner = (FileSelector)d;
				owner.UpdateFileNameDisplay();
				owner.OnPathChanged?.Invoke(owner, null);
			}));
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FileSelector), new FrameworkPropertyMetadata("Choose a file...", null));

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

		public event EventHandler<RoutedEventArgs> OnPathChanged;

		public FileSelector()
		{
			InitializeComponent();
		}

		private void UpdateFileNameDisplay()
		{
			string fileName = FileName;

			if (string.IsNullOrEmpty(fileName))
			{
				txtFileName.Text = string.Empty;
				return;
			}

			txtFileName.Text = fileName;
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();

			dlg.Filter = FileFilter;
			dlg.Title = Title;
			dlg.Multiselect = false;
			bool? result = dlg.ShowDialog();
			if (result == true) FileName = dlg.FileName;
		}
	}
}
