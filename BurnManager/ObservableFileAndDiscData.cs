using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ObservableFileAndDiscData : FileAndDiscData
    {
        private new ObservableFileList _allFiles = new ObservableFileList();
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

        public virtual void Initialize()
        {
            _allFiles.Clear();
            _allVolumes.Clear();
        }

        public ObservableFileAndDiscData(FileAndDiscData data)
        {
            foreach (var item in data.AllFiles) _allFiles.Add(new FileProps(item));
            foreach (var volume in data.AllVolumes) _allVolumes.Add(new VolumeProps(volume));
        }

        public ObservableFileAndDiscData(ObservableFileAndDiscData data)
        {
            foreach (var item in data.AllFiles) _allFiles.Add(new FileProps(item));
            foreach (var volume in data.AllVolumes) _allVolumes.Add(new VolumeProps(volume));
        }

        public static bool operator ==(ObservableFileAndDiscData? a, ObservableFileAndDiscData? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            lock (a.LockObj)
            {
                lock (b.LockObj)
                {
                    bool volumesMatch = false;
                    foreach (VolumeProps volumeA in a.AllVolumes)
                    {
                        foreach (VolumeProps volumeB in b.AllVolumes)
                        {
                            if (volumeA == volumeB)
                            {
                                volumesMatch = true;
                                break;
                            }
                            if (!volumesMatch) return false;
                        }
                    }


                    return (a.AllFiles == b.AllFiles &&
                        //CollectionComparers.CompareLists(a.AllVolumes, b.AllVolumes)) &&
                        a.FormatVersion == b.FormatVersion);
                }
            }
        }
        public static bool operator !=(ObservableFileAndDiscData? a, ObservableFileAndDiscData? b)
        {
            return !(a == b);
        }

    }
}
