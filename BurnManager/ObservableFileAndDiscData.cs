using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ObservableFileAndDiscData : FileAndDiscData
    {
        private ObservableFileList _allFiles = new ObservableFileList();
        public new ObservableFileList AllFiles
        {
            get
            {
                return _allFiles;
            }
        }

        private ObservableCollection<VolumeProps> _allVolumes = new ObservableCollection<VolumeProps>();
        public new ObservableCollection<VolumeProps> AllVolumes
        {
            get
            {
                return _allVolumes;
            }
        }

        public ObservableFileAndDiscData()
        {
        }

        public ObservableFileAndDiscData(FileAndDiscData data)
        {
            foreach (var item in data.AllFiles) _allFiles.Add(new FileProps(item));
            foreach (var volume in data.AllVolumes) _allVolumes.Add(new VolumeProps(volume));
        }
    }
}
