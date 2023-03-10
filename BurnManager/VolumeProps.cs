using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnManager
{
    //Represents a single backup volume (blu ray disc, drive, etc). Operations are atomic.
    public class VolumeProps
    {
        public delegate int IdentifierDelegate(); //a delegate which assigns an identifier to this VolumeProps
        private IdentifierDelegate? _getIdentifier { get; set; }
        public IdentifierDelegate? GetIdentifier
        {
            get
            {
                return _getIdentifier;
            }
        }
        public readonly object LockObj = new object();
        private int _identifier { get; set; } = -1;
        public string Name { get; set; } = "";
        private ulong _capacityInBytes { get; set; }
        private int _timesBurned { get; set; } = 0;
        [JsonIgnore]
        public FileList Files { get; } = new FileList();

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
                    return _capacityInBytes - Files.TotalSizeInBytes;
                }
            }
        }
        public ulong SpaceUsed { 
            get
            {
                lock (LockObj)
                {
                    return Files.TotalSizeInBytes;
                }
            }
        }
        public int Identifier { 
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
                            Files.Add(file);
                        }
                    }
                }
            }
        }

        [JsonConstructor]
        public VolumeProps(IdentifierDelegate GetIdentifier, ulong CapacityInBytes, 
            int Identifier, int TimesBurned, FileList Files)
        {
            lock (LockObj)
            {
                this._getIdentifier = GetIdentifier;
                this._capacityInBytes = CapacityInBytes;
                this._identifier = Identifier;
                this._timesBurned = TimesBurned;
                Files = new FileList();
            }
        }

        public void Add(FileProps file)
        {
            lock (LockObj)
            {
                lock (file.LockObj)
                {
                    if (Files.Contains(file)) return;
                    Files.Add(file);
                    file.RelatedVolumes.Add(new DiscAndBurnStatus { IsBurned = false, VolumeID = _identifier });
                }
            }
        }

        //For use when deserializing. If (skipNewRelationship), adds a file to this VolumeProps without creating
        //a new relationship to this VolumeProps in the file's RelatedVolumes
        public void Add(FileProps file, bool skipNewRelationship)
        {
            lock (LockObj)
            {
                lock (file.LockObj)
                {
                    if (Files.Contains(file)) return;
                    Files.Add(file);
                    if (!skipNewRelationship)
                    {
                        file.RelatedVolumes.Add(new DiscAndBurnStatus { IsBurned = false, VolumeID = _identifier });
                    }
                }
            }
        }

        public async Task AddAsync(FileProps file)
        {
            await Task.Run(() =>
            {
                Add(file);
            });
        }

        //Does not remove references to this VolumeProps from the FileProps being removed
        public bool Remove(FileProps file)
        {
            lock (LockObj)
            {
                lock (file.LockObj)
                {
                    if (Files.Remove(file)) return true;
                }
            }
            return false;
        }

        //Remove the file from this VolumeProps and remove the reference to this VolumeProps from the file
        public bool CascadeRemove(FileProps file)
        {
            bool operationResult = true;
            lock (LockObj)
            {
                lock (file.LockObj)
                {
                    if (Files.Remove(file))
                    {
                        DiscAndBurnStatus? thisDisc = file.RelatedVolumes.Where(a => a.VolumeID == _identifier).First();
                        if (thisDisc == null)
                        {
                            throw new DataException("File did not contain a relationship to this disc.");
                        }
                        file.RelatedVolumes.Remove(thisDisc);
                    }
                    else operationResult = false;
                }
            }
            return operationResult;
        }

        public async Task<bool> RemoveAsync(FileProps file)
        {
            bool result = false;
            await Task.Run(() =>
            {
                result = CascadeRemove(file);
            });
            return result;
        }

        public async Task<bool> Contains(FileProps compare)
        {
            bool result = false;
            await Task.Run(() => {
                lock (LockObj)
                {
                    lock (compare.LockObj)
                    {
                        List<FileProps> results = Files.Where(a => a == compare).ToList();
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

        public void SetIdentifier(int newID)
        {
            lock (LockObj)
            {
                _identifier = newID;
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

        //From a list of VolumeProps, generate an ID that is not used by any existing VolumeProps
        public static int GetNewID(ICollection<VolumeProps> VolumePropsCollection)
        {
            int favorite = 0;
            bool maxValue = false;
            foreach (var volume in VolumePropsCollection)
            {
                if (favorite <= volume.Identifier) favorite = volume.Identifier + 1;
                if (volume.Identifier == int.MaxValue)
                {
                    maxValue = true;
                    break;
                }
            }

            if (maxValue)
            {
                bool foundNewValue = false;
                favorite = 0;
                List<VolumeProps> volumesAsList = VolumePropsCollection.ToList();
                while (!foundNewValue)
                {
                    foreach (var volume in VolumePropsCollection)
                    {
                        if (favorite == volume.Identifier)
                        {
                            favorite++;
                            foundNewValue = false;
                            break;
                        }
                        foundNewValue = true;
                    }
                    if (foundNewValue) break;
                    if (favorite == int.MaxValue) throw new OverflowException("What are you backing up onto " + int.MaxValue + " volumes?");
                }
            }

            return favorite;
        }

        //Returns all VolumeProps matching 'ID'
        public static List<VolumeProps> GetVolumePropsByID(ICollection<VolumeProps> Source, int ID)
        {
            List<VolumeProps> results = new List<VolumeProps>();
            foreach (var volume in Source)
            {
                if (volume.Identifier == ID) results.Add(volume);
            }
            return results;
        }

        public static bool operator ==(VolumeProps? a, VolumeProps? b)
        {
            //if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null ^ b is null) return false;
            if (a is null && b is null) return true;
            if (a.Files.Count() != b.Files.Count()) return false;

            bool fileListsIdentical = false;
            foreach (FileProps fileA in a.Files)
            {
                foreach (FileProps fileB in b.Files)
                {
                    if (fileA == fileB)
                    {
                        fileListsIdentical = true;
                        break;
                    }
                    if (!fileListsIdentical) return false;
                }
            }

            return (a.CapacityInBytes == b.CapacityInBytes &&
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
                    return Files.GetEnumerator();
                }
            }
        }
    }
}