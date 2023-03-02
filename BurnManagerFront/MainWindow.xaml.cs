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
using Microsoft.Win32;

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

            totalSizeOutput_Name.DataContext = api.Data.AllFiles.TotalSizeInBytes;
            listBox.DataContext = api.Data.AllFiles.Files;
            BindingOperations.EnableCollectionSynchronization(api.Data.AllFiles.Files, api.LockObj);
        }

        public void InitializeCallbacks()
        {
            UICallback dataCallback = new UICallback
            {
                Update = () => {
                    lock (_lockObj) lock (api.LockObj) lock (api.Data.AllFiles.LockObj)
                    {
                        //note: can behave erratically if we try to grab data from the api while on the UI thread
                        ulong totalSizeValue = api.Data.AllFiles.TotalSizeInBytes;

                        this.Dispatcher.Invoke(() =>
                        {
                            totalSizeOutput_Name.Content = "Total size (bytes): " + totalSizeValue;
                            totalCountOutput_Name.Content = "Count: (pending optimization!)";// + api.data.AllFiles.Count;
                        });
                    }
                }
            };

            api.Data.AllFiles.OnUpdate = dataCallback.Callback;
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
            _openSaveDialog("asdf!");
        }

        //Push a new operation to _pendingOperations after checking whether a new operation can be added.
        //If it can't add a new operation, it returns null.
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
        
        //Incomplete, return to this after save dialog is working
        private MessageBoxResult _saveChangesDialog()
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                "Unsaved changes have been made, backup records could be lost. Do you want to save?", 
                "BurnManager", MessageBoxButton.YesNoCancel);

            return result;
        }

        //Open the save dialog and save 'serializedData'. Returns false if the operation fails or if the user cancels
        private bool _openSaveDialog(string serializedData)
        {
            try
            {
                Stream aStream;
                SaveFileDialog saveDialog = new SaveFileDialog();

                saveDialog.Filter = "*." + BurnManagerAPI.Extension + "|All Files(*.*)";
                saveDialog.DefaultExt = BurnManagerAPI.Extension;

                if (saveDialog.ShowDialog() == true)
                {
                    if ((aStream = saveDialog.OpenFile()) != null)
                    {
                        byte[] toBytes = new byte[serializedData.Length];
                        for (int i = 0; i < serializedData.Length; i++) toBytes[i] = (byte)serializedData[i];

                        aStream.Write(toBytes);
                        aStream.Close();
                        api.UpdateSavedState();
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show("Exception thrown from _openSaveDialog:\n" + exception);
            }
            return false;
        }

        private void FileNew_MenuClick(object sender, RoutedEventArgs e)
        {
            lock (_lockObj)
            {
                PendingOperation? thisOperation = _pushOperation(true, "Revert to new file");

                if (thisOperation == null) return;
                bool a = api.SavedStateAltered;
                if (api.SavedStateAltered)
                {
                    MessageBoxResult result = _saveChangesDialog();
                    if (result == MessageBoxResult.Yes)
                    {
                        if (_openSaveDialog(api.Serialize()))
                        {
                            api.Initialize();
                        }
                    }
                    if (result == MessageBoxResult.Cancel) return;
                }

                api.Initialize();

                bool saved = api.SavedStateAltered;
                _popOperation(thisOperation);
            }
        }

        private void FileSave_MenuClick(object sender, RoutedEventArgs e)
        {
            lock (_lockObj)
            {
                PendingOperation? thisOperation = _pushOperation(true, "File Save");
                if (thisOperation == null) return;

                string serialized = api.Serialize();
                _openSaveDialog(serialized);

                _popOperation(thisOperation);
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
