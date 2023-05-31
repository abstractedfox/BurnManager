//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

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
        public BurnManagerAPI api;
        public object LockObj = new object();

        //note: This is only a container; each long-running operation is responsible for its own PendingOperation, describing
        //whether that operation should block other long-running operations, and removing its PendingOperation
        //when completed
        private List<PendingOperation> _pendingOperations = new List<PendingOperation>();

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();

            _initializeUI();
        }

        private void _initializeUI()
        {
            Dispatcher.Invoke(() => {
                _initializeCallbacks();
                totalSizeOutput_Name.DataContext = api.Data.AllFiles.TotalSizeInBytes;
                listBox.DataContext = api.Data.AllFiles.Files;
                BindingOperations.EnableCollectionSynchronization(api.Data.AllFiles.Files, api.LockObj);

                burnListBox.DataContext = api.Data.AllVolumes;
                BindingOperations.EnableCollectionSynchronization(api.Data.AllVolumes, api.LockObj);
            });

            if (api.Data.AllFiles.OnUpdate != null)
            {
                api.Data.AllFiles.OnUpdate();
            }
        }

        private void _initializeCallbacks()
        {
            UICallback dataCallback = new UICallback
            {
                Update = () => {
                    lock (LockObj) lock (api.LockObj) lock (api.Data.AllFiles.LockObj)
                    {
                        //note: can behave erratically if we try to grab data from the api while on the UI thread
                        ulong totalSizeValue = api.Data.AllFiles.TotalSizeInBytes;
                        int totalCountValue = api.Data.AllFiles.Count;

                        this.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                totalSizeOutput_Name.Content = "Total size (bytes): " + totalSizeValue;
                                totalCountOutput_Name.Content = "Count: " + totalCountValue;
                                return;
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine("boilerplate");
                            }
                        //}
                        });

                        Console.WriteLine("boilerplate");
                    }
                }
            };

            api.Data.AllFiles.OnUpdate = dataCallback.Callback;
        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation? thisOperation = PushOperation(true, "File Add");
            if (thisOperation == null) return;

            CompletionCallback onComplete = () =>
            {
                PopOperation(thisOperation);
            };

            IReadOnlyList<StorageFile> files = await FrontendFunctions.OpenFilePicker(this);

            await FrontendFunctions.AddStorageFiles(files, onComplete, api);
        }

        private async void AddFolder_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation? thisOperation = PushOperation(true, "Folder Add");
            if (thisOperation == null) return;

            CompletionCallback onComplete = () =>
            {
                PopOperation(thisOperation);
            };

            StorageFolder startingFolder = await FrontendFunctions.OpenFolderPicker(this);
            if (startingFolder == null)
            {
                onComplete();
                return;
            }
            /*
            AddFoldersRecursive folderAdd = new AddFoldersRecursive(api);
            thisOperation.ProcedureInstance = folderAdd;
            folderAdd.callOnCompletionDelegate = onComplete;
            folderAdd.AddFolderToQueue(startingFolder);

            folderAdd.StartOperation();
            folderAdd.EndWhenComplete();*/

            AddFoldersRecursiveAndGenerateChecksums folderAdd = new AddFoldersRecursiveAndGenerateChecksums(api, onComplete);
            thisOperation.ProcedureInstance = folderAdd;
            folderAdd.callOnCompletionDelegate = onComplete;
            folderAdd.AddFolderToQueue(startingFolder);

            folderAdd.StartOperation();
            folderAdd.EndWhenComplete();

            return;


            LinkedList<StorageFolder> nextFolders = new LinkedList<StorageFolder>();
            List<StorageFile> files = new List<StorageFile>();

            nextFolders.AddFirst(startingFolder);
            LinkedListNode<StorageFolder>? currentElement = nextFolders.First;

            while (nextFolders.First != null)
            {
                foreach (var file in await nextFolders.First.Value.GetFilesAsync())
                {
                    files.Add(file);
                }

                foreach (var folder in await nextFolders.First.Value.GetFoldersAsync())
                {
                    nextFolders.AddLast(folder);
                }

                nextFolders.RemoveFirst();
            }

            await FrontendFunctions.AddStorageFiles(files, onComplete, api);
        }

        private async void MainWindow_RemoveFiles_Button_Click(object sender, RoutedEventArgs e)
        {
            await RemoveFilesFromListBox(listBox);
        }

        public async Task RemoveFilesFromListBox(ListBox box)
        {
            List<FileProps> readFrom;
            PendingOperation? thisOperation;
            lock (LockObj)
            {
                thisOperation = PushOperation(true, "Remove Files");
                if (thisOperation == null) return;

                var items = listBox.SelectedItems;
                readFrom = new List<FileProps>();

                foreach (FileProps item in items)
                {
                    readFrom.Add(item);
                }
            }
            foreach (FileProps item in readFrom)
            {
                await api.RemoveFile(item);
            }

            lock (LockObj)
            {
                PopOperation(thisOperation);
            }

        }

        private void VerifyChecksums_ButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation? thisOperation = PushOperation(true, "Verify checksums sequentially");
                if (thisOperation == null) return;

                List<FileProps> errors = BurnManagerAPI.VerifyChecksumsSequential(api.Data.AllFiles);
                if (errors.Count == 0)
                {
                    System.Windows.MessageBox.Show("Verify checksums: No errors found!");
                }
                else
                {
                    System.Windows.MessageBox.Show(errors.Count + " errored files were found.");
                }
                
                PopOperation(thisOperation);
            }
        }

        private void _debugButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                foreach(var operation in _pendingOperations)
                {
                    if (!(operation.ProcedureInstance is null))
                    {
                        operation.ProcedureInstance.EndImmediately();
                    }
                }
            }
        }

        //Push a new operation to _pendingOperations after checking whether a new operation can be added.
        //If it can't add a new operation, it returns null.
        public PendingOperation? PushOperation(bool isBlocking, string name)
        {
            lock (LockObj)
            {
                if (_pendingOperations.Count > 0 && _pendingOperations.Where(operation => operation.Blocking == true).Any())
                {
                    FrontendFunctions.OperationsInProgressDialog(_pendingOperations);
                    return null;
                }
                PendingOperation operation = new PendingOperation(isBlocking, name);
                _pendingOperations.Add(operation);
                return operation;
            }
        }

        public void PopOperation(PendingOperation operation)
        {
            lock (LockObj)
            {
                if (!_pendingOperations.Remove(operation))
                {
                    throw new Exception("Running operation was not registered in _operationsPending");
                }
            }
        }
        

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

                saveDialog.Filter = "*" + BurnManagerAPI.Extension + "|All Files(*.*)";
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

        private string? _openOpenDialog()
        {
            string? output = null;
            try
            {
                Stream aStream;
                OpenFileDialog openDialog = new OpenFileDialog();

                openDialog.Filter = "*" + BurnManagerAPI.Extension + "|All Files(*.*)";
                openDialog.DefaultExt = BurnManagerAPI.Extension;

                if (openDialog.ShowDialog() == true)
                {
                    if ((aStream = openDialog.OpenFile()) != null)
                    {
                        using (StreamReader reader = new StreamReader(aStream))
                        {
                            output = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show("Exception thrown from _openOpenDialog:\n" + exception);
            }
            return output;
        }



        private void FileNew_MenuClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation? thisOperation = PushOperation(true, "Revert to new file");

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
                    if (result == MessageBoxResult.Cancel)
                    {
                        PopOperation(thisOperation);
                        return;
                    }
                }

                api.Initialize();

                bool saved = api.SavedStateAltered;
                PopOperation(thisOperation);
            }
        }

        private void FileSave_MenuClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation? thisOperation = PushOperation(true, "File Save");
                if (thisOperation == null) return;

                string serialized = api.Serialize();
                _openSaveDialog(serialized);

                PopOperation(thisOperation);
            }
        }

        private async void FileOpen_MenuClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => { 
                lock (LockObj)
                {
                    PendingOperation? thisOperation = PushOperation(true, "File Open");
                    if (thisOperation == null) return;

                    if (api.SavedStateAltered)
                    {
                        MessageBoxResult result = _saveChangesDialog();
                        if (result == MessageBoxResult.Yes)
                        {
                            if (!_openSaveDialog(api.Serialize()))
                            {
                                PopOperation(thisOperation);
                                return;
                            }
                        }
                        if (result == MessageBoxResult.Cancel)
                        {
                            PopOperation(thisOperation);
                            return;
                        }
                    }

                    string? serialized = _openOpenDialog();
                    if (serialized == null)
                    {
                        PopOperation(thisOperation);
                        return;
                    }

                    if (api.LoadFromJson(serialized) == ResultCode.SUCCESSFUL)
                    {
                        api.UpdateSavedState();
                        _initializeUI();
                    }

                    PopOperation(thisOperation);
                }
            });
        }

        private async void GenerateBurns_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation? thisOperation;
            ulong volumeSize = 0;
            ulong clusterSize = 0;

            lock (LockObj)
            {
                thisOperation = PushOperation(true, "File Sort");
                if (thisOperation == null) return;

                try
                {
                    volumeSize = ulong.Parse(VolumeSizeInput.Text);
                    clusterSize = ulong.Parse(BlockSizeInput.Text);
                }
                catch (FormatException)
                {
                    System.Windows.MessageBox.Show("Please input a valid number!");
                    PopOperation(thisOperation);
                    return;
                }
                catch (OverflowException)
                {
                    System.Windows.MessageBox.Show("Please insert a value smaller than " + ulong.MaxValue);
                    PopOperation(thisOperation);
                    return;
                }
            }

            List<FileProps> errors = await api.EfficiencySort(clusterSize, volumeSize);


            lock (LockObj)
            {
                PopOperation(thisOperation);
            }
        }

        private void burnListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lock (LockObj)
            {
                VolumeProps? item = (VolumeProps)burnListBox.SelectedItem;
                if (item == null) return;

                VolumePropsDetails detailsWindow = new VolumePropsDetails(this);
                detailsWindow.SetVolumeProps(item);
                detailsWindow.Show();
            }
        }

        private void StageBurn_ButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation? thisOperation = PushOperation(true, "Staging Burn");
                if (thisOperation == null) return;

                VolumeProps? volumeToStage = (VolumeProps)burnListBox.SelectedItem;
                if (volumeToStage == null)
                {
                    System.Windows.MessageBox.Show("Please select a burn to stage.");
                    PopOperation(thisOperation);
                    return;
                }

                string stagingPath = StagingDirectoryInput.Text;
                if (!Directory.Exists(stagingPath))
                {
                    System.Windows.MessageBox.Show("Please input a valid staging directory.");
                    PopOperation(thisOperation);
                    return;
                }

                ResultCode result = 
                    BurnManagerAPI.StageVolumeProps(volumeToStage, stagingPath, false, api.PlatformSpecificDirectorySeparator);

                PopOperation(thisOperation);
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
