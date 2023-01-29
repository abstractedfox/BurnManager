//DataHandling.cs contains all classes for handling data.

using System.Collections;
using System.Security.Cryptography.X509Certificates;

namespace BurnManager
{
    public enum HashType
    {
        none,
        MD5
    }

    public enum FileStatus
    {
        none,
        GOOD,
        CHECKSUM_ERROR,
        FILE_MISSING
    }

    //Used to identify files which are meant to be kept together when algorithmically sorting.
    public class MonolithicCollection
    {
        public string? CollectionName { get; set; } //User-friendly name for this collection (optional)
        public int CollectionID { get; set; } = 0; //0 = not part of a collection
    }

    //Represents a single file
    public class FileProps
    {
        public string? FileName { get; set; }
        public string? OriginalPath { get; set; }
        public ulong? SizeInBytes { get; set; }
        public List<DiscAndBurnStatus> RelatedVolumes { get; set; } = new List<DiscAndBurnStatus>(); //Volumes which this file is or may be burned to
        public Byte[]? Checksum { get; set; }
        public DateTime? LastModified { get; set; } //The last-modified time of this file, from the file system
        public DateTime? TimeAdded { get; set; } //The date & time this instance was created
        public HashType? HashAlgUsed { get; set; }
        public FileStatus? Status { get; set; }
        public MonolithicCollection Monolithic { get; } = new MonolithicCollection();

        //Note: Above types are nullable so instances of this type can be used for partial comparisons
        
        public FileProps()
        {
            TimeAdded = DateTime.Now;
        }

        //If any DiscAndBurnStatus in AssociatedVolumes has IsBurned set to false, return them. Returns an empty collection if there are none.
        public ICollection<DiscAndBurnStatus> GetPendingBurns()
        {
            return (ICollection<DiscAndBurnStatus>)RelatedVolumes.Where(vol => vol.IsBurned == false);
        }

        public static bool PartialEquals(FileProps a, FileProps b)
        {

        }

        public static bool operator ==(FileProps a, FileProps b)
        {
            //Incomplete
            return a == b;
        }

        public static bool operator !=(FileProps a, FileProps b)
        {
            //Incomplete
            return a != b;
        }
    }

    //Represents a single volume (blu ray disc, drive, etc)
    public class VolumeProps
    {
        private string _identifier;

        public ulong SizeInBytes { get; set; }
        public ulong SpaceRemaining { get; set; }
        public ulong SpaceUsed { get; set; }
        public List<FileProps> Files { get; set; }
        public string Identifier { get
            {
                return _identifier;
            }
        }
        public int TimesBurned { get; }
        private delegate string _getIdentifierDelegate(); //a delegate which assigns an identifier to this VolumeProps
        private _getIdentifierDelegate? _identifierGetter { get; set; }

        //Assign a delegate that can produce an identifier
        public void AssignIdentifierDelegate(Delegate identifierFunction)
        {
            _identifierGetter = (_getIdentifierDelegate)identifierFunction;
        }

        //Calls the delegate in identifierGetter to set an identifier for this VolumeProps
        public void SetIdentifier()
        {
            if (HasIdentifierDelegate)
            {
                _identifier = _identifierGetter();
            }
            else
            {
                throw new NullReferenceException("VolumeProps.setIdentifier: identifierGetter is null.");
            }
        }

        public bool HasIdentifierDelegate
        {
            get
            {
                return _identifierGetter != null;
            }
        }
    }

    //Represents a single volume that a FileProps is associated with, and whether it has been burned to that volume
    public class DiscAndBurnStatus
    {
        public VolumeProps Volume { get; set; }
        public bool IsBurned { get; set; }
    }

    //A collection of FileProps.
    public class FileList : ICollection<FileProps>
    {
        //private List<FileProps> _files;
        private HashSet<FileProps> _files = new HashSet<FileProps>();
        private object _lockObject = new object();

        //Get all files in this FileList where at least one DiscAndBurnStatus.IsBurned == false
        public async Task<ICollection<FileProps>> GetPendingFiles()
        {
            List<FileProps> results = new List<FileProps>();
            await Task.Run(() =>
            {
                results = (List<FileProps>)_files.Where(file => file.GetPendingBurns().Count() > 0);
            });
            return results;
        }

        //Returns all FileProps whose attributes match the attributes of 'compareTo', ignoring any null values in compareTo
        public async Task<ICollection<FileProps>> GetFilesByPartialMatch(FileProps compareTo)
        {
            List<FileProps> results = new List<FileProps>();
            await Task.Run(() =>
            {
                results = (List<FileProps>)_files.Where(file => FileProps.PartialEquals(file, compareTo));
            });
            return results;
        }

        //Interface implementations below
        public int Count => _files.Count();
        
        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(FileProps item)
        {
            _files.Add(item);
        }

        public void Clear()
        {
            _files.Clear();
        }

        public bool Contains(FileProps item)
        {
            return _files.Contains(item);
        }

        public void CopyTo(FileProps[] array, int arrayIndex)
        {
            _files.CopyTo(array, arrayIndex);
        }

        public IEnumerator<FileProps> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        public bool Remove(FileProps item)
        {
            return _files.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }
    }
}