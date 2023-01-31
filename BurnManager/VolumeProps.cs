using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnManager
{
    //Represents a single backup volume (blu ray disc, drive, etc). Operations are atomic.
    public class VolumeProps
    {
        public delegate string IdentifierDelegate(); //a delegate which assigns an identifier to this VolumeProps
        private IdentifierDelegate? _getIdentifier { get; set; }
        public IdentifierDelegate? GetIdentifier
        {
            get
            {
                return _getIdentifier;
            }
        }
        public readonly object LockObj = new object();
        private string _identifier { get; set; } = "";
        private ulong _capacityInBytes { get; set; }
        private int _timesBurned { get; set; } = 0;
        public FileList _files { get; } = new FileList();

        public ulong CapacityInBytes { 
            get
            {
                lock (LockObj)
                {
                    return _capacityInBytes;
                }
            }
        }
        public ulong SpaceRemaining { 
            get
            {
                lock (LockObj)
                {
                    return _capacityInBytes - _files.TotalSizeInBytes;
                }
            }
        }
        public ulong SpaceUsed { 
            get
            {
                lock (LockObj)
                {
                    return _files.TotalSizeInBytes;
                }
            }
        }
        public string Identifier { 
            get
            {
                lock (LockObj)
                {
                    return _identifier;
                }
            }
        }
        public int TimesBurned {
            get
            {
                lock (LockObj)
                {
                    return _timesBurned;
                }
            }
        }

        public VolumeProps(ulong sizeInBytes)
        {
            lock (LockObj)
            {
                _capacityInBytes = sizeInBytes;
            }
        }

        public VolumeProps(VolumeProps copySource)
        {
            lock (LockObj)
            {
                lock (copySource.LockObj)
                {
                    AssignIdentifierDelegate(copySource.GetIdentifier);
                    _identifier = copySource.Identifier;
                    _capacityInBytes = copySource.CapacityInBytes;
                    _timesBurned = copySource.TimesBurned;
                    foreach (var file in copySource)
                    {
                        lock (file.LockObj)
                        {
                            _files.Add(file);
                        }
                    }
                }
            }
        }

        [JsonConstructor]
        public VolumeProps(IdentifierDelegate GetIdentifier, ulong CapacityInBytes, 
            string Identifier, int TimesBurned, FileList Files)
        {
            lock (LockObj)
            {
                this._getIdentifier = GetIdentifier;
                this._capacityInBytes = CapacityInBytes;
                this._identifier = Identifier;
                this._timesBurned = TimesBurned;
                this._files = Files;
            }
        }

        public async Task Add(FileProps file)
        {
            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    lock (file.LockObj)
                    {
                        if (_files.Contains(file)) return;
                        _files.Add(file);
                        file.RelatedVolumes.Add(new DiscAndBurnStatus { IsBurned = false, Volume = this });
                    }
                }
            });
        }

        public async Task<bool> Remove(FileProps file)
        {
            bool operationResult = true;
            await Task.Run(() =>
            {
                lock (LockObj)
                {
                    lock (file.LockObj)
                    {
                        if (_files.Remove(file))
                        {
                            DiscAndBurnStatus? thisDisc = file.RelatedVolumes.Where(a => a.Volume == this).First();
                            if (thisDisc == null)
                            {
                                throw new DataException("File did not contain a relationship to this disc.");
                            }
                            file.RelatedVolumes.Remove(thisDisc);
                        }
                        else operationResult = false;
                    }
                }
            });
            return operationResult;
        }

        public async Task<bool> Contains(FileProps compare)
        {
            bool result = false;
            await Task.Run(() => {
                lock (LockObj)
                {
                    lock (compare.LockObj)
                    {
                        List<FileProps> results = _files.Where(a => a == compare).ToList();
                        if (results.Count > 0) result = true;
                    }
                }
            });
            return result;
        }

        public void IncrementBurnCount()
        {
            lock (LockObj)
            {
                _timesBurned++;
            }
        }
        
        public void DecrementBurnCount()
        {
            lock (LockObj)
            {
                _timesBurned--;
            }
        }

        //Assign a delegate that can produce an identifier
        public void AssignIdentifierDelegate(IdentifierDelegate identifierFunction)
        {
            lock (LockObj)
            {
                _getIdentifier = identifierFunction;
            }
        }

        //Calls the delegate in identifierGetter to set an identifier for this VolumeProps
        public void SetIdentifier()
        {
            lock (LockObj)
            {
                if (HasIdentifierDelegate)
                {
                    _identifier = _getIdentifier();
                }
                else
                {
                    throw new NullReferenceException("VolumeProps.SetIdentifier: identifierGetter is null.");
                }
            }
        }

        public bool HasIdentifierDelegate
        {
            get
            {
                lock (LockObj)
                {
                    return _getIdentifier != null;
                }
            }
        }

        public static bool operator ==(VolumeProps? a, VolumeProps? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            List<FileProps> listA = new List<FileProps>();
            List<FileProps> listB = new List<FileProps>();
            foreach (var file in a) listA.Add(file);
            foreach (var file in b) listB.Add(file);

            return (CollectionComparers.CompareLists(listA, listB) &&
                a.CapacityInBytes == b.CapacityInBytes &&
                a.Identifier == b.Identifier &&
                a.TimesBurned == b.TimesBurned);
        }
        public static bool operator !=(VolumeProps? a, VolumeProps? b)
        {
            return !(a == b);
        }

        public IEnumerator<FileProps> GetEnumerator()
        {
            {
                lock (LockObj)
                {
                    return _files.GetEnumerator();
                }
            }
        }
    }
}