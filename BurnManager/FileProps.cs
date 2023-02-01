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
        public DateTime? LastModified { get; set; } //The last-modified time of this file, from the file system
        public List<DiscAndBurnStatus> RelatedVolumes { get; set; } = new List<DiscAndBurnStatus>(); //Volumes which this file is or may be burned to
        public byte[]? Checksum { get; set; }
        public HashType? HashAlgUsed { get; set; }
        public DateTime? TimeAdded { get; set; } //The date & time this instance was created
        public FileStatus? Status { get; set; }
        private MonolithicCollection _monolithic { get; set; } = new MonolithicCollection();
        public MonolithicCollection Monolithic
        {
            get
            {
                return _monolithic;
            }
        }

        public readonly object LockObj = new object();

        //Note: Properties are nullable so instances of this type can be used for partial comparisons with PartialEquals(...)
        
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

            //thought: this could also be a huge boolean expression, but do we want a huge boolean expression?
            if (a.FileName != null && b.FileName != null && a.FileName != b.FileName) return false;
            if (a.OriginalPath != null && b.OriginalPath != null && a.OriginalPath != b.OriginalPath) return false;
            if (a.SizeInBytes != null && b.SizeInBytes != null && a.SizeInBytes != b.SizeInBytes) return false;
            if (a.LastModified != null && b.LastModified != null && a.LastModified != b.LastModified) return false;
            if (!CollectionComparers.CompareLists(a.RelatedVolumes, b.RelatedVolumes)) return false;
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
            return (a.FileName == b.FileName &&
                a.OriginalPath == b.OriginalPath &&
                a.SizeInBytes == b.SizeInBytes &&
                a.LastModified == b.LastModified &&
                CollectionComparers.CompareLists(a.RelatedVolumes, b.RelatedVolumes) &&
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

        public char[] Serialize(char spacer)
        {
            int numOfElements = 11;
            int checksumLength = Checksum.Length;
            int bufferSize = FileName.Length + OriginalPath.Length + (sizeof(ulong)/sizeof(char)) + LastModified.Value.ToString().Length +
                (sizeof(int)/sizeof(char)) + checksumLength + (sizeof(HashType) / sizeof(char)) + TimeAdded.Value.ToString().Length + 
                sizeof(FileStatus) + Monolithic.CollectionName.Length + sizeof(int) / sizeof(char) + (sizeof(char) * numOfElements);

            Console.WriteLine("Buffer size: " + bufferSize);


            //elements in order are: FileName, OriginalPath, SizeInBytes, LastModified, length of checksum, checksum, HashAlgUsed,
            //TimeAdded,Status, Monolithic.CollectionName, Monolithic.CollectionID. Last value added is space for the 'spacer' character
            //between each element

            char[] serialized = new char[bufferSize];
            int charCount = 0;

            Action addSpacer = () =>
            {
                serialized[charCount] = spacer;
                charCount++;
            };

            Action<string> addStringToBuffer = (input) =>
            {
                input.CopyTo(0, serialized, charCount, input.Length);
                charCount += input.Length;
                addSpacer();
            };


            addStringToBuffer(FileName);
            addStringToBuffer(OriginalPath);

            serialized[charCount] = (char)(SizeInBytes >> 32); //high
            serialized[charCount + 1] = (char)(SizeInBytes & int.MaxValue); //low
            charCount += 2;
            addSpacer();

            addStringToBuffer(LastModified.Value.ToString());

            serialized[charCount] = (char)checksumLength;
            charCount++;
            addSpacer();
            Array.Copy(Checksum, 0, serialized, charCount, checksumLength);
            charCount += checksumLength;
            addSpacer();

            serialized[charCount] = (char)HashAlgUsed;
            charCount++;
            addSpacer();

            addStringToBuffer(TimeAdded.Value.ToString());

            serialized[charCount] = (char)Status;
            charCount++;
            addSpacer();

            addStringToBuffer(Monolithic.CollectionName);

            serialized[charCount] = (char)Monolithic.CollectionID;
            addSpacer();

            return serialized;
        }
    }
}