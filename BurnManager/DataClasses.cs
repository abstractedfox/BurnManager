//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

//DataClasses.cs, contains smaller structs that may not need to each be in their own files

using System.Text.Json;

namespace BurnManager
{
    public delegate void CompletionCallback();

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
        FILE_MISSING,
        DUPLICATE,
        BAD_DATA,
        ACCESS_ERROR
    }

    public enum ResultCode
    {
        none,
        SUCCESSFUL,
        UNSUCCESSFUL,
        NULL_VALUE,
        INVALID_JSON,
        DUPLICATE,
        INVALID_PATH,
        LOG_ALREADY_EXISTS,
        FINISHED_WITH_ERRORS
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

    //Describes a pending operation. This is intended for the convenience of the implementation
    public class PendingOperation{
        public object LockObj = new object();
        private bool _blocking = false;
        public string? Name;
        public bool Blocking
        {
            get
            {
                return _blocking;
            }
        }
        
        //isBlocking should be used to track whether this operation should block other operations from starting.
        //However, blocking must be implemented by the caller
        public PendingOperation(bool isBlocking)
        {
            _blocking = isBlocking;
        }

        public PendingOperation(bool isBlocking, string name)
        {
            _blocking = isBlocking;
            Name = name;
        }
    }

    public class Constants
    {
        public enum Type
        {
            uninitialized,
            VolumePropsOutputIdentifier
        };
        public const string UndefinedData = "|UndefinedData|";
        public const string VolumePropsOutputIdentifier = "|VolumePropsOutputFile|";
        public const string VolumePropsOutputFilename = "Log.txt";

        public static string GetConstant(Type type)
        {
            if (type == Type.VolumePropsOutputIdentifier) return VolumePropsOutputIdentifier;
            return UndefinedData;
        }

        public static Type? IsConstant(string input)
        {
            if (input == VolumePropsOutputIdentifier) return Type.VolumePropsOutputIdentifier;
            return null;
        }
    }


    //A class to be used when creating an output file from a VolumeProps. This does not produce data for serialization
    //of the main file store, it's for the reference file to be included with each burn.
    class VolumePropsSummaryOutput
    {
        public string name { get; } = "Uninitialized VolumePropsSummaryOutput";
        public int volumeID { get; } = -1;
        public List<Tuple<string, byte[]>> data { get; } = new List<Tuple<string, byte[]>>();

        public VolumePropsSummaryOutput(VolumeProps volume)
        {
            name = volume.Name;
            volumeID = volume.Identifier;
            foreach (var file in volume)
            {
                if (file.OriginalPath == null)
                {
                    file.Status = FileStatus.BAD_DATA;
                }
                if (file.Checksum == null)
                {
                    file.Status = FileStatus.CHECKSUM_ERROR;
                }

                data.Add(new Tuple<string, byte[]>(file.OriginalPath, file.Checksum));
            }
        }
    }
}