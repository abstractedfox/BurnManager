using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ObservableFileList : FileList
    {
        private ObservableCollection<FileProps> _files = new ObservableCollection<FileProps>();
        public new ObservableCollection<FileProps> Files
        {
            get
            {
                return _files;
            }
        }
    }
}
