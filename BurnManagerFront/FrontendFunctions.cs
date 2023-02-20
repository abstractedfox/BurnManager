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
                FileName = file.DisplayName,
                OriginalPath = file.Path,
                LastModified = (await file.GetBasicPropertiesAsync()).DateModified,
                Status = status,
                TimeAdded = DateTimeOffset.Now
            };
        }

        //Generates checksums for all passed files. Errored files will be returned in errorOutput
        //Only put this here in case putting it in the same binary as the ui fixed our little permissions problem but it looks like it doesn't
        public static async Task GenerateChecksums(ICollection<FileProps> files, bool overwriteExistingChecksum, ICollection<FileProps> errorOutput)
        {
            object _lockObj = new object();
            await Task.Run(() => {
                ParallelLoopResult result = Parallel.ForEach(files, file => {
                    using (MD5 hashtime = MD5.Create())
                    {
                        lock (file.LockObj)
                        {
                            if (!overwriteExistingChecksum && file.HasChecksum) return;
                            if (!BurnManagerAPI.FileExists(file))
                            {
                                lock (_lockObj)
                                {
                                    file.Status = FileStatus.FILE_MISSING;
                                    errorOutput.Add(file);
                                    return;
                                }
                            }
                            try
                            {
                                file.Checksum = hashtime.ComputeHash(new FileStream(file.OriginalPath, FileMode.Open));

                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.WriteLine("AAAAA");
                            }
                        }
                    }
                });
            });
        }
    }
}