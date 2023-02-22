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


namespace BurnManager
{
    //Most runtime program logic interfaces through this class
    public class BurnManagerAPI
    {
        public ObservableFileAndDiscData data;
        public ObservableFileAndDiscData lastSavedState;
        public object LockObj = new object();

        public BurnManagerAPI()
        {
            data = new ObservableFileAndDiscData();
            lastSavedState = data;
        }

        public string Serialize()
        {
            string jsonString = JsonSerializer.Serialize(data);
            return jsonString;
        }
        //Loads a Json-serialized FileAndDiscData into this instance's 'data' and 'lastSavedState'
        //Only handles loading; does not check whether the saved state has been altered first
        public ResultCode LoadFromJson(string serializedJson)
        {
            ResultCode operationResult = 0;
            ObservableFileAndDiscData newData = new ObservableFileAndDiscData(JsonToFileAndDiscData(serializedJson, ref operationResult));
            if (operationResult == ResultCode.SUCCESSFUL)
            {
                lock (LockObj)
                {
                    data = newData;
                    lastSavedState = data;
                }
                return ResultCode.SUCCESSFUL;
            }
            else return operationResult;
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


        //===================File and sorting operations
        public void AddFile(FileProps file)
        {
            lock (LockObj)
            {
                data.AllFiles.Add(file);
            }
        }
        public void NewVolume(ulong sizeInBytes)
        {

        }
        public void RemoveFile(FileProps file)
        {

        }
        public void RemoveVolume(VolumeProps volume)
        {

        }

        //Sort files efficiently across the smallest possible number of volumes
        public void EfficiencySort()
        {

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
                                
                            }
                            catch (UnauthorizedAccessException)
                            {
                                AccessError(file);
                            }
                            catch (IOException)
                            {
                                //Note, this can be thrown if the file is locked by another program but could also be caused if we have
                                //having fun concurrency issues
                                AccessError(file);
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
                ParallelLoopResult result = Parallel.ForEach(files, file => {
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
        public static void VerifyChecksums(FileList files)
        {

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
                TimeAdded = DateTime.Now,
                LastModified = DateTime.Now,
                OriginalPath = "c:\\testPropsA",
                SizeInBytes = 500,
                Status = FileStatus.GOOD
            };
            FileProps testPropsB = new FileProps
            {
                Checksum = new byte[] { 1, 2, 3, 4 },
                FileName = "testPropsB",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTime.Now,
                LastModified = DateTime.MinValue,
                OriginalPath = "e:\\testPropsB",
                SizeInBytes = 300,
                Status = FileStatus.FILE_MISSING
            };
            FileProps testPropsC = new FileProps
            {
                Checksum = new byte[] { 3, 3, 3, 3 },
                FileName = "testPropsC",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTime.Now,
                LastModified = DateTime.MaxValue,
                OriginalPath = "e:\\asdf\\testPropsC",
                SizeInBytes = 600,
                Status = FileStatus.FILE_MISSING
            };
            FileProps testPropsD = new FileProps
            {
                Checksum = new byte[] { 4, 3, 3, 9, 4, 7, 5, 6 },
                FileName = "testPropsD",
                HashAlgUsed = HashType.MD5,
                TimeAdded = DateTime.Now,
                LastModified = DateTime.MaxValue,
                OriginalPath = "e:\\asdf\\testPropsD",
                SizeInBytes = 700,
                Status = FileStatus.FILE_MISSING
            };
            VolumeProps volPropsA = new VolumeProps(100000000);
            VolumeProps volPropsB = new VolumeProps(987654321);
            await volPropsA.AddAsync(testPropsA); //500bytes
            await volPropsA.AddAsync(testPropsB); //300bytes
            await volPropsB.AddAsync(testPropsC);

            data.AllFiles.Add(testPropsA);
            data.AllFiles.Add(testPropsB);
            data.AllFiles.Add(testPropsC);
            data.AllFiles.Add(testPropsD);
            data.AllVolumes.Add(volPropsA);
            data.AllVolumes.Add(volPropsB);

        }
    }
}
