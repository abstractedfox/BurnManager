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
using System.Windows.Data;
using Windows.Storage.Pickers;
using Windows.Devices.Bluetooth.Advertisement;
using System.IO;

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
            BindingOperations.EnableCollectionSynchronization(api.data.AllFiles.Files, api.LockObj);
        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<StorageFile> files = await FrontendFunctions.OpenFilePicker(this);
            List<FileProps> filesToChecksum = new List<FileProps>();
            List<FileProps> erroredFiles = new List<FileProps>();
            int count = 0;

            ChecksumFactory hashtime = new ChecksumFactory();
            hashtime.StartQueue();
            

            foreach (StorageFile file in files)
            {
                FileProps filePropped = new FileProps(await FrontendFunctions.StorageFileToFileProps(file));
                await Dispatcher.InvokeAsync(async () => api.AddFile(filePropped));
                filesToChecksum.Add(filePropped);
                count++;
                if (count > 100)
                {
                    //BurnManagerAPI.GenerateChecksums(new List<FileProps>(filesToChecksum), true, erroredFiles);
                    hashtime.AddBatch(filesToChecksum);
                    filesToChecksum.Clear();
                    count = 0;
                }
            }

            //await BurnManagerAPI.GenerateChecksums(filesToChecksum, true, erroredFiles);

            if (filesToChecksum.Count > 0) hashtime.AddBatch(filesToChecksum);
            hashtime.FinishQueue();
        }
    }
}
