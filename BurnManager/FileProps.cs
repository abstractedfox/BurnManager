using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnManager
{
    //Represents a single file
    public class FileProps
    {
        public string? FileName { get; set; }
        public string? OriginalPath { get; set; }
        public ulong? SizeInBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; } //The last-modified time of this file, from the file system
        public List<DiscAndBurnStatus>? RelatedVolumes { get; set; } = new List<DiscAndBurnStatus>(); //Volumes which this file is or may be burned to
        public byte[]? Checksum { get; set; }
        public HashType? HashAlgUsed { get; set; }
        public DateTimeOffset? TimeAdded { get; set; } //The date & time this instance was created
        public FileStatus? Status { get; set; }
        private MonolithicCollection _monolithic { get; set; } = new MonolithicCollection();
        public MonolithicCollection Monolithic
        {
            get
            {
                return _monolithic;
            }
        }
        //Note: Properties are nullable so instances of this type can be used for partial comparisons with PartialEquals(...)

        public readonly object LockObj = new object();

        //Note that this only checks whether the Checksum array is not null & contains more than one member
        public bool HasChecksum
        {
            get
            {
                return (Checksum != null && Checksum.Length > 1);
            }
        }

        
        public FileProps()
        {
        }

        public FileProps(FileProps copySource)
        {
            lock (LockObj)
            {
                lock (copySource.LockObj)
                {
                    FileName = copySource.FileName;
                    OriginalPath = copySource.OriginalPath;
                    SizeInBytes = copySource.SizeInBytes;
                    LastModified = copySource.LastModified;
                    RelatedVolumes = new List<DiscAndBurnStatus>(copySource.RelatedVolumes);
                    Checksum = copySource.Checksum;
                    HashAlgUsed = copySource.HashAlgUsed;
                    TimeAdded = copySource.TimeAdded;
                    Status = copySource.Status;
                    _monolithic = new MonolithicCollection(copySource.Monolithic);
                }
            }
        }

        [JsonConstructor]
        public FileProps(string? FileName, string? OriginalPath, ulong? SizeInBytes, DateTime? LastModified,
            List<DiscAndBurnStatus> RelatedVolumes, byte[]? Checksum, HashType? HashAlgUsed, DateTime? TimeAdded,
            FileStatus? Status, MonolithicCollection Monolithic)
        {
            lock (LockObj) { 
                this.FileName = FileName;
                this.OriginalPath = OriginalPath;
                this.SizeInBytes = SizeInBytes;
                this.LastModified = LastModified;
                this.RelatedVolumes = RelatedVolumes;
                this.Checksum = Checksum;
                this.HashAlgUsed = HashAlgUsed;
                this.TimeAdded = TimeAdded;
                this.Status = Status;
                this._monolithic = Monolithic;
            }
        }

        //If any DiscAndBurnStatus in AssociatedVolumes has IsBurned set to false, return them. Returns an empty collection if there are none.
        public ICollection<DiscAndBurnStatus> GetPendingBurns()
        {
            return RelatedVolumes.Where(vol => vol.IsBurned == false).ToList();
        }

        //Only compares properties that are not null. Use this to check equality only for desired properties.
        public static bool PartialEquals(FileProps a, FileProps b)
        {
            if (a == null || b == null) return false;

            if (!(a.RelatedVolumes is null ^ b.RelatedVolumes is null))
            {
                foreach (DiscAndBurnStatus relationshipA in a.RelatedVolumes)
                {
                    bool result = false;
                    foreach (DiscAndBurnStatus relationshipB in b.RelatedVolumes)
                    {
                        if (relationshipA == relationshipB)
                        {
                            result = true;
                            break;
                        }
                    }
                    if (!result) return false;
                }
            }

            //thought: this could also be a huge boolean expression, but do we want a huge boolean expression?
            if (a.FileName != null && b.FileName != null && a.FileName != b.FileName) return false;
            if (a.OriginalPath != null && b.OriginalPath != null && a.OriginalPath != b.OriginalPath) return false;
            if (a.SizeInBytes != null && b.SizeInBytes != null && a.SizeInBytes != b.SizeInBytes) return false;
            if (a.LastModified != null && b.LastModified != null && a.LastModified != b.LastModified) return false;
            //if (!CollectionComparers.CompareLists(a.RelatedVolumes, b.RelatedVolumes)) return false;
            if (a.Checksum != null && b.Checksum != null && !CollectionComparers.CompareByteArrays(a.Checksum, b.Checksum)) return false;
            if (a.HashAlgUsed != null && b.HashAlgUsed != null && a.HashAlgUsed != b.HashAlgUsed) return false;
            if (a.TimeAdded != null && b.TimeAdded != null && a.TimeAdded != b.TimeAdded) return false;
            if (a.Status != null && b.Status != null && a.Status != b.Status) return false;
            if (a.Monolithic != null && b.Monolithic != null && a.Monolithic != b.Monolithic) return false;
            return true;
        }

        public static bool operator ==(FileProps? a, FileProps? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;

            bool relationshipCompare = false;
            foreach (DiscAndBurnStatus relationships1 in a.RelatedVolumes)
            {
                foreach (DiscAndBurnStatus relationships2 in b.RelatedVolumes)
                {
                    if (relationships1 == relationships2) relationshipCompare = true;
                    break;
                }
                if (!relationshipCompare) return false;
            }

            return (a.FileName == b.FileName &&
                a.OriginalPath == b.OriginalPath &&
                a.SizeInBytes == b.SizeInBytes &&
                a.LastModified == b.LastModified &&
                CollectionComparers.CompareByteArrays(a.Checksum, b.Checksum) &&
                a.HashAlgUsed == b.HashAlgUsed &&
                a.TimeAdded == b.TimeAdded &&
                a.Status == b.Status &&
                a.Monolithic == b.Monolithic);
        }

        public static bool operator !=(FileProps a, FileProps b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            /*
            if (ReferenceEquals(obj, null))
            {
                return false;
            }*/

            return false;
        }

    }
}