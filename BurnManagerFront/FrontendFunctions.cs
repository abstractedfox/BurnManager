//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

using BurnManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;


namespace BurnManagerFront
{
    internal class FrontendFunctions
    {
        public static async Task<IReadOnlyList<StorageFile>> OpenFilePicker(System.Windows.Window parentWindow)
        {
            //Microsoft.Win32.OpenFileDialog picker = new Microsoft.Win32.OpenFileDialog();
            //bool? result = picker.ShowDialog
            IReadOnlyList<StorageFile> files = new List<StorageFile>();

            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;


            var winapimoment = new System.Windows.Interop.WindowInteropHelper(parentWindow);
            IntPtr handle = winapimoment.Handle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
            files = await picker.PickMultipleFilesAsync();
            return files;
        }

        public static async Task<FileProps> StorageFileToFileProps(StorageFile file)
        {
            FileStatus status = 0;
            if (file.IsAvailable) status = FileStatus.GOOD;
            return new FileProps
            {
                FileName = file.Name,
                OriginalPath = file.Path,
                SizeInBytes =(await file.GetBasicPropertiesAsync()).Size,
                LastModified = (await file.GetBasicPropertiesAsync()).DateModified,
                Status = status,
                TimeAdded = DateTimeOffset.Now
            };
        }

        public static void OperationsInProgressDialog()
        {
            System.Windows.MessageBox.Show("BurnManager is busy, please wait for the current operation to complete.");
        }

        public static void OperationsInProgressDialog(ICollection<PendingOperation> operations)
        {
            string output = "BurnManager is busy with the following operations:\n";
            int length = output.Length;
            foreach(var item in operations)
            {
                if (item.Name != null)
                {
                    output += item.Name + "\n";
                }
            }
            if (length == output.Length)
            {
                OperationsInProgressDialog();
                return;
            }

            System.Windows.MessageBox.Show(output + "\nPlease wait for these operations to complete.");
        }

        
    }
}