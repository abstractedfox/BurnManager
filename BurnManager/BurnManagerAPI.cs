//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.IsolatedStorage;


using System.Security.Principal;
using System.Security.Cryptography;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;

namespace BurnManager
{
    //Most runtime program logic interfaces through this class
    public class BurnManagerAPI
    {
        public ObservableFileAndDiscData Data;
        private ObservableFileAndDiscData _lastSavedState;
        public object LockObj = new object();
        public const string Extension = ".burnmanager";
        public char PlatformSpecificDirectorySeparator = '\\';

        public BurnManagerAPI()
        {
            lock (LockObj)
            {
                Data = new ObservableFileAndDiscData();
                UpdateSavedState();
            }
        }

        //===================Meta features related to this struct

        public string Serialize()
        {
            string jsonString = JsonSerializer.Serialize(Data);
            return jsonString;
        }

        //Loads a Json-serialized FileAndDiscData into this instance's 'data' and 'lastSavedState'
        //Only handles loading; does not check whether the saved state has been altered first
        public ResultCode LoadFromJson(string serializedJson)
        {
            ResultCode operationResult = 0;
            //await Task.Run(() => { 
                ObservableFileAndDiscData newData = new ObservableFileAndDiscData(JsonToFileAndDiscData(serializedJson, ref operationResult));
                if (operationResult == ResultCode.SUCCESSFUL)
                {
                    lock (LockObj)
                    {
                        Data = newData;
                        Data.PopulateVolumes();
                        _lastSavedState = new ObservableFileAndDiscData(Data);
                    }
                    operationResult = ResultCode.SUCCESSFUL;
                }
                else operationResult = ResultCode.UNSUCCESSFUL;
            //});
            return operationResult;
        }

        //Returns a new FileAndDiscData from a JSON string. Passed ResultCode in arg2 is used to return the result of the operation
        public static FileAndDiscData JsonToFileAndDiscData(string serializedJson, ref ResultCode operationResultOut)
        {
            FileAndDiscData? newData;

            if (serializedJson == null)
            {
                operationResultOut = ResultCode.NULL_VALUE;
                return new FileAndDiscData();
            }

            try
            {
                newData = JsonSerializer.Deserialize<FileAndDiscData>(serializedJson);
            }
            catch (JsonException)
            {
                operationResultOut = ResultCode.INVALID_JSON;
                return new FileAndDiscData();
            }
            catch (InvalidOperationException)
            {
                operationResultOut = ResultCode.INVALID_JSON;
                return new FileAndDiscData();
            }

            if (newData == null)
            {
                operationResultOut = ResultCode.UNSUCCESSFUL;
                return new FileAndDiscData();
            }

            operationResultOut = ResultCode.SUCCESSFUL;
            return newData;
        }

        public bool SavedStateAltered
        {
            get
            {
                lock (LockObj)
                {
                    return Data != _lastSavedState;
                }
            }
        }

        //Update the lastSavedState instance to reflect the current value of 'data'. To be called after saving to a file.
        public void UpdateSavedState()
        {
            lock (LockObj)
            {
                _lastSavedState = new ObservableFileAndDiscData(Data);
            }
        }

        public void Initialize()
        {
            Data.Initialize();
            UpdateSavedState();
        }

        //===================File and sorting operations
        public ResultCode AddFile(FileProps file)
        {
            lock (LockObj)
            {
                //Debug.WriteLine("boilerplate" + data.AllFiles.TotalSizeInBytes);
                Data.AllFiles.Add(file);
            }
            return ResultCode.SUCCESSFUL;
        }
        public void NewVolume(ulong sizeInBytes)
        {

        }
        public async Task RemoveFile(FileProps file)
        {
            await Data.AllFiles.CascadeRemove(file, Data.AllVolumes, LockObj);
        }
        public void RemoveVolume(VolumeProps volume)
        {

        }

        //Sorts files in AllFiles to fit efficiently across a list of new VolumeProps,
        //and replaces the contents of Data.AllVolumes with the new set of VolumeProps.
        public async Task<List<FileProps>> EfficiencySort(ulong clusterSize, ulong volumeSize)
        {
            List<FileProps> errors = new List<FileProps>();
            await Task.Run(() => { 
                lock (LockObj)
                {
                    List<VolumeProps> sorted = Sorting.SortForEfficientDistribution(Data.AllFiles, clusterSize, volumeSize, true, out errors);

                    foreach (var volume in Data.AllVolumes)
                    {
                        volume.CascadeClear();
                    }
                    Data.AllVolumes.Clear();

                    foreach (var item in sorted) Data.AllVolumes.Add(item);

                }
            });

            FileProps a = Data.AllVolumes.First().Files.Files[Constants.VolumePropsOutputIdentifier];

            return errors;
        }

        //Generates checksums for all passed files. Errored files will be returned in errorOutput
        public static async Task GenerateChecksums(ICollection<FileProps> files, bool overwriteExistingChecksum, ICollection<FileProps> errorOutput)
        {
            object _lockObj = new object();
            await Task.Run(() => { 
                ParallelLoopResult result = Parallel.ForEach(files, file => {
                    using (MD5 hashtime = MD5.Create())
                    {
                        lock (file.LockObj)
                        {

                            if (!overwriteExistingChecksum && file.HasChecksum) return;
                            if (!FileExists(file))
                            {
                                lock (_lockObj)
                                {
                                    file.Status = FileStatus.FILE_MISSING;
                                    errorOutput.Add(file);
                                    return;
                                }
                            }
                            try
                            {
                                file.Checksum = hashtime.ComputeHash(new FileStream(file.OriginalPath, FileMode.Open));
                                file.HashAlgUsed = HashType.MD5;
                                
                            }
                            catch (UnauthorizedAccessException)
                            {
                                AccessError(file);
                                file.Status = FileStatus.ACCESS_ERROR;
                                errorOutput.Add(file);
                            }
                            catch (IOException)
                            {
                                //Note, this can be thrown if the file is locked by another program but could also be caused if we have
                                //having fun concurrency issues
                                AccessError(file);
                                file.Status = FileStatus.ACCESS_ERROR;
                                errorOutput.Add(file);
                            }
                        }
                    }
                });
            });
        }

        public static async Task GenerateChecksums(ICollection<Tuple<FileProps, byte[]>> files, bool overwriteExistingChecksum, ICollection<FileProps> errorOutput)
        {
            object _lockObj = new object();
            await Task.Run(() => {
                ParallelLoopResult result = Parallel.ForEach(files, 
                    //new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1},
                    file => {
                        using (MD5 hashtime = MD5.Create())
                        {
                            lock (file.Item1.LockObj)
                            {
                                if (!overwriteExistingChecksum && file.Item1.HasChecksum) return;
                                if (!FileExists(file.Item1))
                                {
                                    lock (_lockObj)
                                    {
                                        file.Item1.Status = FileStatus.FILE_MISSING;
                                        errorOutput.Add(file.Item1);
                                        return;
                                    }
                                }
                                try
                                {
                                    file.Item1.Checksum = hashtime.ComputeHash(file.Item2);
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    AccessError(file.Item1);
                                }
                            }
                        }
                });
            });
        }


        //===================Data operations

        //Perform all verifications on the list of passed files
        public static void VerifyFiles(FileList files)
        {

        }

        //Verify the existing checksum of a list of files one-by-one. Mostly for making sure there were no sync problems
        //when generating checksums concurrently
        public static List<FileProps> VerifyChecksumsSequential(ICollection<FileProps> files)
        {
            List<FileProps> erroredFiles = new List<FileProps>();
            foreach (var file in files)
            {
                using (MD5 hashtime = MD5.Create())
                {
                    try
                    {
                        byte[] compareChecksum = hashtime.ComputeHash(new FileStream(file.OriginalPath, FileMode.Open));
                        if (!Enumerable.SequenceEqual(compareChecksum, file.Checksum))
                        {
                            erroredFiles.Add(file);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        AccessError(file);
                    }
                    catch (IOException)
                    {
                        AccessError(file);
                    }
                }
            }
            return erroredFiles;
        }
        public static void VerifyFilesExist(FileList files)
        {

        }
        public static bool FileExists(FileProps file)
        {
            return file.OriginalPath == null || File.Exists(file.OriginalPath);
        }
        //Verify the integrity of file and volume relationships, and check for discrepancies
        public static void VerifyDataIntegrity(FileAndDiscData data)
        {

        }

        public static ResultCode StageVolumeProps(VolumeProps volume, string path, bool skipLogFile, char platformSpecificDirectorySeparator)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return ResultCode.INVALID_PATH;

            if (path.Last() != platformSpecificDirectorySeparator)
            {
                path += platformSpecificDirectorySeparator;
            }

            string outputFilePath = path + volume.Name + Constants.VolumePropsOutputFilename;
            if (File.Exists(outputFilePath)) return ResultCode.LOG_ALREADY_EXISTS;

            bool errorsFound = false;
            foreach (var file in volume)
            {
                if (file.OriginalPath == Constants.VolumePropsOutputIdentifier)
                {
                    if (!skipLogFile)
                    {
                        File.WriteAllText(outputFilePath, file.FileName);
                    }
                    continue;
                }

                if (File.Exists(file.OriginalPath))
                {
                    File.Copy(file.OriginalPath, path + file.FileName);
                }
                else
                {
                    file.Status = FileStatus.FILE_MISSING;
                }
            }
            if (errorsFound) return ResultCode.FINISHED_WITH_ERRORS;
            return ResultCode.SUCCESSFUL;
        }

        //===================Events and handlers
        //Call when the user attempts to remove a file which has been marked as burned
        public static void RemoveBurnedFile(FileProps file)
        {

        }
        //Call when the user attempts to remove a volume which has been burned
        public static void RemoveBurnedVolume(VolumeProps volume)
        {

        }
        //Call to respond to a data error
        public static void DataError(FileStatus status, FileProps file)
        {

        }
        public static void AccessError(FileProps file)
        {

        }

        //Remove a pending operation from a list. Mostly useful when wrapped in a delegate as a callback for an asynchronous operation
        public static bool RemovePendingOperation(ICollection<PendingOperation> Operations, PendingOperation operationToRemove)
        {
            return Operations.Remove(operationToRemove);
        }

        //===================Other
        //Save the string in 'contents' to the path at 'path.'
        public static void SaveFile(string contents, string path)
        {

        }

        //===================Test functions

        public async Task TestState()
        {
            await FillDummyData();
        }

        //Populate structs with dummy data for testing purposes.
        public async Task FillDummyData()
        {
            FileProps testPropsA = new FileProps
            {
                Checksum = new byte[] { 1, 1, 1, 1 },
                FileName = "testPropsA",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTimeOffset.Now,
                LastModified = DateTimeOffset.Now,
                OriginalPath = "c:\\testPropsA",
                SizeInBytes = 500,
                Status = FileStatus.GOOD
            };
            FileProps testPropsB = new FileProps
            {
                Checksum = new byte[] { 1, 2, 3, 4 },
                FileName = "testPropsB",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTimeOffset.Now,
                LastModified = DateTimeOffset.MinValue,
                OriginalPath = "e:\\testPropsB",
                SizeInBytes = 300,
                Status = FileStatus.FILE_MISSING
            };
            FileProps testPropsC = new FileProps
            {
                Checksum = new byte[] { 3, 3, 3, 3 },
                FileName = "testPropsC",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTimeOffset.Now,
                LastModified = DateTimeOffset.MaxValue,
                OriginalPath = "e:\\asdf\\testPropsC",
                SizeInBytes = 600,
                Status = FileStatus.FILE_MISSING
            };
            FileProps testPropsD = new FileProps
            {
                Checksum = new byte[] { 4, 3, 3, 9, 4, 7, 5, 6 },
                FileName = "testPropsD",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTimeOffset.Now,
                LastModified = DateTimeOffset.MaxValue,
                OriginalPath = "e:\\asdf\\testPropsD",
                SizeInBytes = 700,
                Status = FileStatus.FILE_MISSING
            };
            VolumeProps volPropsA = new VolumeProps(100000000);
            VolumeProps volPropsB = new VolumeProps(987654321);
            volPropsA.SetIdentifier(1);
            volPropsB.SetIdentifier(2);
            volPropsA.Name = "volPropsA";
            volPropsB.Name = "volPropsB";
            await volPropsA.AddAsync(testPropsA); //500bytes
            await volPropsA.AddAsync(testPropsB); //300bytes
            await volPropsB.AddAsync(testPropsC);

            Data.AllFiles.Add(testPropsA);
            Data.AllFiles.Add(testPropsB);
            Data.AllFiles.Add(testPropsC);
            Data.AllFiles.Add(testPropsD);
            Data.AllVolumes.Add(volPropsA);
            Data.AllVolumes.Add(volPropsB);

        }
    }
}
