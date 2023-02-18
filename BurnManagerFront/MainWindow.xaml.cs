using BurnManager;
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

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace BurnManagerFront
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BurnManagerAPI api;

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();
            //api.TestState();
            DataContext = api.data.AllFiles.Files;

        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<StorageFile> files = await FrontendFunctions.OpenFilePicker(this);

            await Task.Run(async () => { 
                foreach (StorageFile file in files)
                {
                    api.AddFile(await FrontendFunctions.StorageFileToFileProps(file));
                }
            });
        }
    }
}
