﻿//Copyright 2023 Chris/abstractedfox.
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

        private string _statusIndicatorUI = "Status: Uninitialized";
        private PendingOperationsWPF _pendingOperations;

        public MainWindow()
        {
            InitializeComponent();
            api = new BurnManagerAPI();
            _pendingOperations = new PendingOperationsWPF(this);

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

                statusOutputLabel.DataContext = _statusIndicatorUI;
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
                            }
                        });
                    }
                }
            };

            api.Data.AllFiles.OnUpdate = dataCallback.Callback;
        }

        private async void AddFiles_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation thisOperation = new PendingOperation(true, "File Add");
            if (!_pendingOperations.Add(thisOperation))
            {
                return;
            }

            Action onComplete = () =>
            {
                _pendingOperations.Remove(thisOperation);
            };

            IReadOnlyList<StorageFile> files = await FrontendFunctions.OpenFilePicker(this);

            await FrontendFunctions.AddStorageFiles(files, onComplete, api);
        }

        private async void AddFolder_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation thisOperation = new PendingOperation(true, "Folder Add");
            if (!_pendingOperations.Add(thisOperation))
            {
                return;
            }

            Action onComplete = () =>
            {
                _pendingOperations.Remove(thisOperation);
                
            };

            StorageFolder startingFolder = await FrontendFunctions.OpenFolderPicker(this);
            if (startingFolder == null)
            {
                onComplete();
                return;
            }

            BurnManager.AddFoldersRecursiveAndGenerateChecksums folderAdd = new BurnManager.AddFoldersRecursiveAndGenerateChecksums(api, onComplete);
            thisOperation.ProcedureInstance = folderAdd;
            folderAdd.callOnCompletionDelegate = onComplete;
            folderAdd.AddFolderToQueue(new DirectoryInfo(startingFolder.Path));

            folderAdd.StartOperation();
            folderAdd.EndWhenComplete();

            return;
        }

        private async void MainWindow_RemoveFiles_Button_Click(object sender, RoutedEventArgs e)
        {
            await RemoveFilesFromListBox(listBox);
        }

        public async Task RemoveFilesFromListBox(ListBox box)
        {
            List<FileProps> readFrom;
            PendingOperation thisOperation;
            lock (LockObj)
            {
                thisOperation = new PendingOperation(true, "Remove Files");
                if (!_pendingOperations.Add(thisOperation))
                {
                    return;
                }

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
                _pendingOperations.Remove(thisOperation);
            }

        }

        private void VerifyChecksums_ButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation thisOperation = new PendingOperation(true, "Verify checksums sequentially");
                if (!_pendingOperations.Add(thisOperation))
                {
                    return;
                }

                List<FileProps> errors = BurnManagerAPI.VerifyChecksumsSequential(api.Data.AllFiles);
                if (errors.Count == 0)
                {
                    System.Windows.MessageBox.Show("Verify checksums: No errors found!");
                }
                else
                {
                    System.Windows.MessageBox.Show(errors.Count + " errored files were found.");
                }
                
                _pendingOperations.Remove(thisOperation);
            }
        }

        private void _debugButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                if (CancelOperationButton.IsEnabled)
                {
                    CancelOperationButton.IsEnabled = false;
                }
                else
                {
                    CancelOperationButton.IsEnabled = true;
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
                PendingOperation thisOperation = new PendingOperation(true, "Revert to new file");
                if (!_pendingOperations.Add(thisOperation)) return;

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
                        _pendingOperations.Remove(thisOperation);
                        return;
                    }
                }

                api.Initialize();

                bool saved = api.SavedStateAltered;
                _pendingOperations.Remove(thisOperation);
            }
        }

        private void FileSave_MenuClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation thisOperation = new PendingOperation(true, "File Save");
                if (!_pendingOperations.Add(thisOperation)) return;

                string serialized = api.Serialize();
                _openSaveDialog(serialized);

                _pendingOperations.Remove(thisOperation);
            }
        }

        private async void FileOpen_MenuClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => { 
                lock (LockObj)
                {
                    PendingOperation thisOperation = new PendingOperation(true, "File Open");
                    bool addSucceeded = _pendingOperations.Add(thisOperation);

                    if (!addSucceeded)
                    {
                        return;
                    }

                    if (api.SavedStateAltered)
                    {
                        MessageBoxResult result = _saveChangesDialog();
                        if (result == MessageBoxResult.Yes)
                        {
                            if (!_openSaveDialog(api.Serialize()))
                            {
                                _pendingOperations.Remove(thisOperation);
                                return;
                            }
                        }
                        if (result == MessageBoxResult.Cancel)
                        {
                            _pendingOperations.Remove(thisOperation);
                            return;
                        }
                    }

                    string? serialized = _openOpenDialog();
                    if (serialized == null)
                    {
                        _pendingOperations.Remove(thisOperation);
                        return;
                    }

                    if (api.LoadFromJson(serialized) == ResultCode.SUCCESSFUL)
                    {
                        api.UpdateSavedState();
                        _initializeUI();
                    }

                    _pendingOperations.Remove(thisOperation);
                }
            });
        }

        private async void GenerateBurns_ButtonClick(object sender, RoutedEventArgs e)
        {
            PendingOperation thisOperation;
            ulong volumeSize = 0;
            ulong clusterSize = 0;

            lock (LockObj)
            {
                thisOperation = new PendingOperation(true, "File Sort");
                if (!_pendingOperations.Add(thisOperation)) return;

                try
                {
                    volumeSize = ulong.Parse(VolumeSizeInput.Text);
                    clusterSize = ulong.Parse(BlockSizeInput.Text);
                }
                catch (FormatException)
                {
                    System.Windows.MessageBox.Show("Please input a valid number!");
                    _pendingOperations.Remove(thisOperation);
                    return;
                }
                catch (OverflowException)
                {
                    System.Windows.MessageBox.Show("Please insert a value smaller than " + ulong.MaxValue);
                    _pendingOperations.Remove(thisOperation);
                    return;
                }
            }

            List<FileProps> errors = await api.EfficiencySort(clusterSize, volumeSize);


            lock (LockObj)
            {
                _pendingOperations.Remove(thisOperation);
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
                PendingOperation thisOperation = new PendingOperation(true, "Staging Burn");
                if (!_pendingOperations.Add(thisOperation)) return;

                VolumeProps? volumeToStage = (VolumeProps)burnListBox.SelectedItem;
                if (volumeToStage == null)
                {
                    System.Windows.MessageBox.Show("Please select a burn to stage.");
                    _pendingOperations.Remove(thisOperation);
                    return;
                }

                string stagingPath = StagingDirectoryInput.Text;
                if (!Directory.Exists(stagingPath))
                {
                    System.Windows.MessageBox.Show("Please input a valid staging directory.");
                    _pendingOperations.Remove(thisOperation);
                    return;
                }

                ResultCode result = 
                    BurnManagerAPI.StageVolumeProps(volumeToStage, stagingPath, false, api.PlatformSpecificDirectorySeparator);

                _pendingOperations.Remove(thisOperation);
            }
        }

        public void UpdateStatusIndicator(string status)
        {
            lock (LockObj)
            {
                Dispatcher.Invoke(() => { 
                    statusOutputLabel.Content = "Status: " + status;
                });
            }
        }

        private void CancelOperation_ButtonClick(object sender, RoutedEventArgs e)
        {
            _pendingOperations.Cancel();
        }

        private void AddMissingChecksums_ButtonClick(object sender, RoutedEventArgs e)
        {
            lock (LockObj)
            {
                PendingOperation checksumOperation = new PendingOperation(true);
                checksumOperation.ProcedureInstance = BurnManagerAPI.FillMissingChecksums(api.Data.AllFiles);

                checksumOperation.ProcedureInstance.callOnCompletionDelegate = () =>
                {
                    lock (LockObj)
                    {
                        _pendingOperations.Remove(checksumOperation);
                    }
                };

                bool addOperationSucceeded = _pendingOperations.Add(checksumOperation);

                if (!addOperationSucceeded)
                {
                    return;
                }

                checksumOperation.ProcedureInstance.StartOperation();
                checksumOperation.ProcedureInstance.EndWhenComplete();
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
