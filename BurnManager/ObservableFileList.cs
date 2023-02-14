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
                    _filesObservable.Add(item);
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

        public override async Task<bool> CascadeRemove(FileProps item, ICollection<VolumeProps> relatedVolumes)
        {
            bool result = false;
            result = await base.CascadeRemove(item, relatedVolumes);

            lock (item.LockObj)
            {
                if (result)
                {
                    lock (LockObj)
                    {
                        return _filesObservable.Remove(item);
                    }
                }
            }
            return false;
        }
    }

}
