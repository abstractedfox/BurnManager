//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ObservableFileList : FileList
    {
        protected ObservableCollection<FileProps> _filesObservable = new ObservableCollection<FileProps>();

        public new ObservableCollection<FileProps> Files
        {
            get
            {
                return _filesObservable;
            }
        }

        public override void Add(FileProps item)
        {
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    base.Add(item);
                    if (_files.ContainsKey(item.OriginalPath) && item.Status != FileStatus.DUPLICATE)
                    {
                        _filesObservable.Add(item);
                    }
                }
            }
        }

        public override bool Remove(FileProps item)
        {
            lock (LockObj)
            {
                lock (item.LockObj)
                {
                    if (base.Remove(item))
                    {
                        if (_filesObservable.Remove(item))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public new void Clear()
        {
            base.Clear();
            _filesObservable.Clear();
        }

        public override async Task<ResultCode> CascadeRemove(FileProps item, ICollection<VolumeProps> relatedVolumes, object? volumeCollectionLockObj)
        {
            ResultCode result = ResultCode.none;
            result = await base.CascadeRemove(item, relatedVolumes, volumeCollectionLockObj);
            if (result != ResultCode.SUCCESSFUL) return result;

            if (volumeCollectionLockObj == null) volumeCollectionLockObj = new object();

            lock (item.LockObj) lock (volumeCollectionLockObj)
            {
                if (result == ResultCode.SUCCESSFUL)
                {
                    lock (LockObj)
                    {
                        if (_filesObservable.Remove(item)) return ResultCode.SUCCESSFUL;
                        return ResultCode.UNSUCCESSFUL;
                    }
                }
            }
            return result;
        }
    }

}
