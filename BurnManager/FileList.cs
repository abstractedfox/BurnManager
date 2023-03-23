//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BurnManager
{
    //A collection of FileProps. All operations on the backing store are atomic.
    public class FileList : ICollection<FileProps>
    {
        public readonly object LockObj = new object();
        public Action? OnUpdate { get; set; }
        protected Dictionary<string, FileProps> _files = new Dictionary<string, FileProps>();

        private ulong _totalSizeInBytesValue;
        protected ulong _totalSizeInBytes
        {
            get
            {
                return _totalSizeInBytesValue;
            }
            set
            {
                lock (LockObj)
                {
                    _totalSizeInBytesValue = value;
                    if (!(OnUpdate is null)) OnUpdate();
                }
            }
        }
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
        public virtual Dictionary<String, FileProps> Files
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
                            if (file.OriginalPath == null)
                            {
                                file.Status = FileStatus.BAD_DATA;
                                continue;
                            }
                            _files.Add(file.OriginalPath, file);
                        }
                    }
                    _totalSizeInBytes = copySource.TotalSizeInBytes;
                }
            }
        }

        [JsonConstructor]
        public FileList(Dictionary<String, FileProps> Files, ulong TotalSizeInBytes)
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
                    results = (List<FileProps>)_files.Where(file => file.Value.GetPendingBurns().Count() > 0);
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
                    results = (List<FileProps>)_files.Values.Where(file => FileProps.PartialEquals(file, compareTo)).ToList();
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

        //Duplicates will not be added, and will have their Status fields set to DUPLICATE
        public virtual void Add(FileProps item)
        {
            if (item == null)
            {
                throw new NullReferenceException();
            }
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    if (item.OriginalPath == null)
                    {
                        item.Status = FileStatus.BAD_DATA;
                        return;
                    }
                    if (_files.TryAdd(item.OriginalPath, item))
                    {
                        if (item.SizeInBytes != null)
                        {
                            _totalSizeInBytes += (ulong)item.SizeInBytes;
                        }
                    }
                    else
                    {
                        item.Status = FileStatus.DUPLICATE;
                    }
                }
            }
        }

        public virtual ResultCode Add(KeyValuePair<string, FileProps> item)
        {
            if (item.Key == null || item.Value == null)
            {
                throw new NullReferenceException();
            }
            lock (LockObj)
            {
                lock (item.Value.LockObj)
                {
                    if (item.Value.OriginalPath == null) return ResultCode.NULL_VALUE;

                    if (_files.TryAdd(item.Key, item.Value))
                    {
                        if (item.Value.SizeInBytes != null) _totalSizeInBytes += (ulong)item.Value.SizeInBytes;
                        return ResultCode.SUCCESSFUL;  
                    }
                }
            }
            return ResultCode.UNSUCCESSFUL;
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
                if (item.OriginalPath == null) return false;
                return _files.ContainsKey(item.OriginalPath) && _files[item.OriginalPath] == item;
            }
        }

        public void CopyTo(FileProps[] array, int arrayIndex)
        {
            lock (LockObj)
            {
                _files.Values.CopyTo(array, arrayIndex);
            }
        }

        //This could be changed to return keys, but aside from keeping compatibility with code that may already expect to receive FileProps
        //that doesn't seem like it would actually help performance in any way since you'd then need to look up each member by 
        //key anyway
        public IEnumerator<FileProps> GetEnumerator()
        {
            lock (LockObj)
            {
                return _files.Values.GetEnumerator();
            }
        }

        //Remove a file from this list. This will Not remove relationships to this file from volumes in its RelatedVolumes struct
        //If removing from a top level file list (ie a list that is meant to track all files) use CascadeRemove
        //Note that both Remove functions don't check whether a file is marked as burned, they only care about data functionality
        public virtual bool Remove(FileProps item)
        {
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    if (item.OriginalPath == null) return false;
                    if (_files.Remove(item.OriginalPath))
                    {
                        if (item.SizeInBytes != null) _totalSizeInBytes -= (ulong)item.SizeInBytes;
                        return true;
                    }
                }
            }
            return false;
        }

        //The passed collection of VolumeProps will be searched for references to the removed file, removing it from those volumes as well
        //Because relatedVolumes is a collection type with no integrated lock object, a lock object can be passed in arg3
        public virtual async Task<ResultCode> CascadeRemove(FileProps item, ICollection<VolumeProps> relatedVolumes, object? volumeCollectionLockObj)
        {
            ResultCode operationResult = ResultCode.UNSUCCESSFUL;
            if (volumeCollectionLockObj == null) volumeCollectionLockObj = new object();

            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    lock (item.LockObj) lock (volumeCollectionLockObj)
                    {
                        if (item.OriginalPath == null)
                        {
                            operationResult = ResultCode.NULL_VALUE;
                            return;
                        }
                        if (_files.Remove(item.OriginalPath))
                        {
                            if (item.SizeInBytes != null) _totalSizeInBytes -= (ulong)item.SizeInBytes;
                            if (item.RelatedVolumes == null)
                            {
                                operationResult = ResultCode.SUCCESSFUL;
                                return;
                            }
                            foreach (var relationship in item.RelatedVolumes)
                            {
                                VolumeProps volume = VolumeProps.GetVolumePropsByID(relatedVolumes, relationship.VolumeID).First();
                                volume.Remove(item);
                            }
                            operationResult = ResultCode.SUCCESSFUL;
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
                    if (file.Value.SizeInBytes != null) _totalSizeInBytes += (ulong)file.Value.SizeInBytes;
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