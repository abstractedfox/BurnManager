//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

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

        public new void PopulateVolumes()
        {
            Func<int, bool> volumeExists = volID =>
            {
                foreach (var volume in _allVolumes)
                {
                    if (volume.Identifier == volID) return true;
                }
                return false;
            };

            //await Task.Run(() => {
                lock (LockObj)
                {
                    foreach (var file in AllFiles)
                    {
                        foreach (var relationship in file.RelatedVolumes)
                        {
                            List<VolumeProps> volume = VolumeProps.GetVolumePropsByID(AllVolumes, relationship.VolumeID);
                            if (volume.Count > 1)
                            {
                                throw new InvalidDataException("Multiple volumes found with the same ID.");
                            }
                            if (volume.Count < 1)
                            {
                                throw new InvalidDataException("No volumes found with the ID " + relationship.VolumeID);
                            }

                            volume.First().Add(file, true);
                        }
                    }
                }
            //});
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
