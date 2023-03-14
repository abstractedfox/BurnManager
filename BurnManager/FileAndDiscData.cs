//using System.Reflection.Metadata.Ecma335;
//using System.Security.Cryptography.X509Certificates;

using System.Text.Json.Serialization;

namespace BurnManager
{
    //Top level struct, this is what contains all the files and volumes we're tracking and what will get passed to the serializer
    public class FileAndDiscData
    {
        protected FileList _allFiles { get; set; } = new FileList();
        public FileList AllFiles { get
            {
                return _allFiles;
            }
        }

        private List<VolumeProps> _allVolumes = new List<VolumeProps>();
        public List<VolumeProps> AllVolumes { get
            {
                return _allVolumes;
            }
        }
        public int FormatVersion { get; } = 1;
        public readonly object LockObj = new object();

        public FileAndDiscData()
        {
        }

        [JsonConstructor]
        public FileAndDiscData(FileList AllFiles, List<VolumeProps> AllVolumes)
        {
            _allFiles = AllFiles;
            _allVolumes = AllVolumes;
        }

        public FileAndDiscData(FileAndDiscData copySource)
        {
            foreach (var file in copySource.AllFiles) AllFiles.Add(file);
            foreach (var volume in copySource.AllVolumes) AllVolumes.Add(volume);
        }

        public virtual void Initialize()
        {
            _allFiles.Clear();
            _allVolumes.Clear();
        }


        //Populate the DiscAndBurnStatus struct in each FileProps that is in _allVolumes
        //Unused, deletion candidate
        public void PopulateFileAndDiscRelationships()
        {
            //await Task.Run(() =>
            //{
                lock (LockObj)
                {
                    foreach (var volume in _allVolumes)
                    {
                        foreach (var file in volume.Files)
                        {
                            lock (file.LockObj)
                            {
                                file.RelatedVolumes.Clear();
                                file.RelatedVolumes.Add(new DiscAndBurnStatus { IsBurned = false, VolumeID = volume.Identifier });
                            }
                        }
                    }
                }
            //});
        }

        //Generate all VolumeProps referenced by the RelatedVolumes struct of each file
        //Because the json serializer doesn't preserve C# references, this must be run after deserialization to rebuild volumes
        public void PopulateVolumes()
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

        public static bool operator ==(FileAndDiscData? a, FileAndDiscData? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            lock (a.LockObj)
            {
                lock (b.LockObj)
                {
                    bool volumesMatch = false;
                    foreach(VolumeProps volumeA in a.AllVolumes)
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
        public static bool operator !=(FileAndDiscData? a, FileAndDiscData? b)
        {
            return !(a == b);
        }
    }
}