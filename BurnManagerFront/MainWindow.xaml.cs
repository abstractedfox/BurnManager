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
        private List<PendingOperation> _pendingOperations = new List<PendingOperation>();

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();

            InitializeCallbacks();

            totalSizeOutput_Name.DataContext = api.data.AllFiles.TotalSizeInBytes;
            listBox.DataContext = api.data.AllFiles.Files;
            BindingOperations.EnableCollectionSynchronization(api.data.AllFiles.Files, api.LockObj);
        }

        public void InitializeCallbacks()
        {
            UICallback dataCallback = new UICallback
            {
                Update = () => {
                    lock (_lockObj)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            totalSizeOutput_Name.Content = "Total size (bytes): " + api.data.AllFiles.TotalSizeInBytes;
                            totalCountOutput_Name.Content = "Count: (pending optimization!)";// + api.data.AllFiles.Count;
                        });
                    }
                }
            };

            api.data.AllFiles.OnUpdate = dataCallback.Callback;
        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation? thisOperation = _pushOperation(true, "File Add");
            if (thisOperation == null) return;

            CompletionCallback onComplete = () =>
            {
                _popOperation(thisOperation);
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

        private async void RemoveFiles_Button_Click(object sender, RoutedEventArgs e)
        {
            PendingOperation? thisOperation = _pushOperation(true, "Remove Files");
            if (thisOperation == null) return;

            var items = listBox.SelectedItems;
            List<FileProps> readFrom = new List<FileProps>();
            foreach (FileProps item in items)
            {
                readFrom.Add(item);
            }

            foreach (FileProps item in readFrom)
            {
                await api.RemoveFile(item);
            }

            _popOperation(thisOperation);
        }

        private void _debugButtonClick(object sender, RoutedEventArgs e)
        {
            List<FileProps> badChecksums = BurnManagerAPI.VerifyChecksumsSequential(api.data.AllFiles);
            System.Windows.MessageBox.Show("Bad checksums result: " + badChecksums.Count);
        }

        private PendingOperation? _pushOperation(bool isBlocking, string name)
        {
            lock (_lockObj)
            {
                if (_pendingOperations.Count > 0)
                {
                    FrontendFunctions.OperationsInProgressDialog(_pendingOperations);
                    return null;
                }
                PendingOperation operation = new PendingOperation(isBlocking, name);
                _pendingOperations.Add(operation);
                return operation;
            }
        }

        private void _popOperation(PendingOperation operation)
        {
            lock (_lockObj)
            {
                if (!_pendingOperations.Remove(operation))
                {
                    throw new Exception("Running operation was not registered in _operationsPending");
                }
            }
        }
    }


    public class UICallback
    {
        private Action trigger;
        public Action Callback
        {
            get
            {
                return trigger;
            }
        }
        public Action? Update
        {
            get;
            set;
        }

        public UICallback(){
            trigger = () =>
            {
                if (!(Update is null))
                {
                    Update();
                }
            };
        }
    }
}
