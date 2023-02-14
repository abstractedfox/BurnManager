using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;

namespace BurnManagerFront
{
    internal class FrontendFunctions
    {
        public static async void OpenFilePicker(System.Windows.Window parentWindow)
        {

            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;

            var winapimoment = new System.Windows.Interop.WindowInteropHelper(parentWindow);
            IntPtr handle = winapimoment.Handle;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
        }
    }
}
