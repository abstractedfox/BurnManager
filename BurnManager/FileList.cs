using System.Collections;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace BurnManager
{
    //A collection of FileProps. All operations on the backing store are atomic.
    public class FileList : ICollection<FileProps>
    {
        public readonly object LockObj = new object();

        //private List<FileProps> _files;
        private HashSet<FileProps> _files = new HashSet<FileProps>();
        private ulong _totalSizeInBytes;
        public ulong TotalSizeInBytes
        {
            get
            {
                lock (LockObj)
                {
                    return _totalSizeInBytes;
                }
            }
        }

        //yes, this is for the json serializer
        public HashSet<FileProps> Files
        {
            get
            {
                return _files;
            }
        }

        public FileList()
        {
        }

        public FileList(FileList copySource)
        {
            lock (LockObj)
            {
                lock (copySource.LockObj)
                {
                    foreach (var file in copySource)
                    {
                        lock (file.LockObj)
                        {
                            _files.Add(file);
                        }
                    }
                    _totalSizeInBytes = copySource.TotalSizeInBytes;
                }
            }
        }

        [JsonConstructor]
        public FileList(HashSet<FileProps> Files, ulong TotalSizeInBytes)
        {
            lock (LockObj)
            {
                this._files = Files;
                RecalculateTotalSize();
            }
        }

        //Get all files in this FileList where at least one DiscAndBurnStatus.IsBurned == false
        public async Task<ICollection<FileProps>> GetPendingFiles()
        {
            List<FileProps> results = new List<FileProps>();
            /*
            results = (List<FileProps>)await GetFilesByPartialMatch(new FileProps { 
                RelatedVolumes = new List<DiscAndBurnStatus> { 
                    new DiscAndBurnStatus { IsBurned = true } } });

            return results;*/
            
            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    results = (List<FileProps>)_files.Where(file => file.GetPendingBurns().Count() > 0);
                }
            });
            return results;
        }

        //Returns all FileProps whose properties match all non-null properties of 'compareTo'
        public async Task<ICollection<FileProps>> GetFilesByPartialMatch(FileProps compareTo)
        {
            List<FileProps> results = new List<FileProps>();
            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    results = (List<FileProps>)_files.Where(file => FileProps.PartialEquals(file, compareTo)).ToList();
                }
            });
            return results;
        }

        //Interface implementations below
        public int Count {
            get
            {
                lock (LockObj)
                {
                    return _files.Count();
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(FileProps item)
        {
            if (item == null)
            {
                throw new NullReferenceException();
            }
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    _files.Add(item);
                    if (item.SizeInBytes != null) _totalSizeInBytes += (ulong)item.SizeInBytes;
                }
            }
        }

        public void Clear()
        {
            lock (LockObj)
            {
                _files.Clear();
                _totalSizeInBytes = 0;
            }
        }

        public bool Contains(FileProps item)
        {
            lock (LockObj)
            {
                return _files.Contains(item);
            }
        }

        public void CopyTo(FileProps[] array, int arrayIndex)
        {
            lock (LockObj)
            {
                _files.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<FileProps> GetEnumerator()
        {
            lock (LockObj)
            {
                return _files.GetEnumerator();
            }
        }

        //Remove a file from this list. This will Not remove relationships to this file from volumes in its RelatedVolumes struct
        //If removing from a top level file list (ie a list that is meant to track all files) use CascadeRemove
        //Note that both Remove functions don't check whether a file is marked as burned, they only care about data functionality
        public bool Remove(FileProps item)
        {
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    if (_files.Remove(item))
                    {
                        if (item.SizeInBytes != null) _totalSizeInBytes -= (ulong)item.SizeInBytes;
                        return true;
                    }
                }
            }
            return false;
        }

        //The passed collection of VolumeProps will be searched for references to the removed file, removing it from those volumes as well
        public async Task<bool> CascadeRemove(FileProps item, ICollection<VolumeProps> relatedVolumes)
        {
            bool operationResult = false;
            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    lock (item.LockObj)
                    {
                        if (_files.Remove(item))
                        {
                            foreach (var relationship in item.RelatedVolumes)
                            {
                                VolumeProps volume = VolumeProps.GetVolumePropsByID(relatedVolumes, relationship.VolumeID).First();
                                volume.Remove(item);
                            }
                            operationResult = true;
                        }
                    }
                }
            });
            return operationResult;
        }

        //Recalculates the _totalSizeInBytes value. Necessary after deserialization
        public void RecalculateTotalSize()
        {
            lock (LockObj)
            {
                foreach (var file in _files)
                {
                    if (file.SizeInBytes != null) _totalSizeInBytes += (ulong)file.SizeInBytes;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (LockObj)
            {
                return _files.GetEnumerator();
            }
        }

        public static bool operator ==(FileList? a, FileList? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            if (a.Count != b.Count) return false;
            lock (a.LockObj)
            {
                lock (b.LockObj)
                {
                    //return (Enumerable.SequenceEqual(a, b));
                    //return CollectionComparers.CompareFileLists(a, b);

                    //Deep compare using FileProps' comparison operator is needed; comparing using ICollection.Contains
                    //appears to only compare references which would cause inaccurate results if comparing two
                    //different instances with identical contents
                    FileList compareA = new FileList(a);
                    FileList compareB = new FileList(b);
                    while (compareA.Count > 0)
                    {
                        bool foundMatch = false;
                        foreach (var fileA in compareA)
                        {
                            foreach (var fileB in compareB)
                            {
                                if (fileA == fileB)
                                {
                                    compareA.Remove(fileA);
                                    compareB.Remove(fileB);
                                    foundMatch = true;
                                    break;
                                }
                            }
                            break;
                        }
                        if (!foundMatch) return false;
                    }
                    return true;
                }
            }
        }
        public static bool operator !=(FileList? a, FileList? b)
        {
            return !(a == b);
        }
    }
}