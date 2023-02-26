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
        private object _lockObj = new object();

        //note: This is only a container; each long-running operation is responsible for its own PendingOperation, describing
        //whether that operation should block other long-running operations, and removing its PendingOperation
        //when completed
        private List<PendingOperation> _operationsPending = new List<PendingOperation>();

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();

            DataContext = api.data.AllFiles.Files;
            BindingOperations.EnableCollectionSynchronization(api.data.AllFiles.Files, api.LockObj);
        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation thisOperation = new PendingOperation(true);
            lock (_lockObj)
            {
                if (_operationsPending.Count > 0)
                {
                    FrontendFunctions.OperationsInProgress();
                    return;
                }
                _operationsPending.Add(thisOperation);
            }

            CompletionCallback onComplete = () =>
            {
                lock (_lockObj)
                {
                    if (!_operationsPending.Remove(thisOperation)) {
                        throw new Exception("Running operation was not registered in _operationsPending");
                    }
                }
            };

            IReadOnlyList<StorageFile> files = await FrontendFunctions.OpenFilePicker(this);
            List<FileProps> filesToChecksum = new List<FileProps>();
            List<FileProps> erroredFiles = new List<FileProps>();
            int count = 0;

            ChecksumFactory hashtime = new ChecksumFactory();
            hashtime.callOnCompletionDelegate = onComplete;
            hashtime.StartQueue();
            

            foreach (StorageFile file in files)
            {
                FileProps filePropped = new FileProps(await FrontendFunctions.StorageFileToFileProps(file));

                //note: Dispatcher.InvokeAsync is necessary because ObservableCollection cannot be modified by 
                //threads other than the one that created it
                await Dispatcher.InvokeAsync(async () => api.AddFile(filePropped));

                filesToChecksum.Add(filePropped);
                count++;
                if (count > 100)
                {
                    hashtime.AddBatch(filesToChecksum);
                    filesToChecksum.Clear();
                    count = 0;
                }
            }

            if (filesToChecksum.Count > 0) hashtime.AddBatch(filesToChecksum);
            hashtime.FinishQueue();

            //while (!hashtime.IsComplete) ; //prevent returning until we implement a real event pattern
        }


        private void DebugButtonClick(object sender, RoutedEventArgs e)
        {
            List<FileProps> badChecksums = BurnManagerAPI.VerifyChecksumsSequential(api.data.AllFiles);
            System.Windows.MessageBox.Show("Bad checksums result: " + badChecksums.Count);
        }
    }
}
