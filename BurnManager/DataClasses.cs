//DataClasses.cs, contains smaller structs that may not need to each be in their own files

//using System.Reflection.Metadata.Ecma335;
//using System.Security.Cryptography.X509Certificates;

using System.Text.Json;

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

    public enum ResultCode
    {
        none,
        SUCCESSFUL,
        UNSUCCESSFUL,
        NULL_VALUE,
        INVALID_JSON
    }

    //Used to identify files which are meant to be kept together when algorithmically sorting.
    public class MonolithicCollection
    {
        public string CollectionName { get; set; } = ""; //User-friendly name for this collection (optional)
        public int CollectionID { get; set; } = 0; //0 = not part of a collection

        public MonolithicCollection()
        { 
        }

        public MonolithicCollection(MonolithicCollection copySource)
        {
            CollectionName = copySource.CollectionName;
            CollectionID = copySource.CollectionID;
        }

        public static bool operator ==(MonolithicCollection? a, MonolithicCollection? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            return (a.CollectionName == b.CollectionName && a.CollectionID == b.CollectionID);
        }
        public static bool operator !=(MonolithicCollection? a, MonolithicCollection? b)
        {
            return !(a == b);
        }
    }

    //Represents a single volume that a FileProps is associated with, and whether it has been burned to that volume
    public class DiscAndBurnStatus
    {
        public int VolumeID { get; set; }
        public bool IsBurned { get; set; }
        public readonly object LockObj = new object();

        public static bool operator ==(DiscAndBurnStatus? a, DiscAndBurnStatus? b)
        {
            if (a is null && !(b is null) || !(a is null) && b is null) return false;
            if (a is null && b is null) return true;
            lock (a.LockObj)
            {
                lock (b.LockObj)
                {
                    return (a.VolumeID == b.VolumeID && a.IsBurned == b.IsBurned);
                }
            }
            //note: volumes should not be compared directly here, or every field will be compared, which will compare every
            //FileProps, which will compare every DiscAndBurnStatus, which will compare the volume
        }
        public static bool operator !=(DiscAndBurnStatus? a, DiscAndBurnStatus? b)
        {
            return !(a == b);
        }
    }
}