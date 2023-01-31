using System.Collections;

namespace BurnManager
{
    //A collection of FileProps. All operations on the backing store are atomic.
    public class FileList : ICollection<FileProps>
    {
        //private List<FileProps> _files;
        private HashSet<FileProps> _files = new HashSet<FileProps>();
        public readonly object LockObj = new object();
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
                    results = (List<FileProps>)_files.Where(file => FileProps.PartialEquals(file, compareTo));
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
        //If removing from a top level file list (ie a list that is meant to track all files) use the overload
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

        //If removeFromVolumes == true, this file will also be removed from any volumes in its RelatedVolumes struct
        public bool Remove(FileProps item, bool removeFromVolumes)
        {
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    if (_files.Remove(item))
                    {
                        if (removeFromVolumes)
                        {
                            foreach (var relationship in item.RelatedVolumes)
                            {
                                relationship.Volume.Remove(item);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
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
            lock (a.LockObj)
            {
                lock (b.LockObj)
                {
                    return (Enumerable.SequenceEqual(a, b));
                }
            }
        }
        public static bool operator !=(FileList? a, FileList? b)
        {
            return !(a == b);
        }
    }
}