//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

//Boilerplate main for testing purposes.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnManager
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            TestDataTypes();
            TestAPI();
            TestJSONSerializer();
            TestJSONSerializerBigger();
            //TestFileListOverride();
            //TestDescendingSort();
            //TestSorting();
            while (true) ; //prevent returning from main if awaited calls cause flow control to continue here

            Console.WriteLine("Plate boiled");
        }

        static void TestSorting()
        {
            Console.WriteLine("boilerplate alert! remember to put in a valid local directory & set a breakpoint at the end");
            string path = "\\\\CHRISSERVER\\Data\\Etc\\buildings and spaces";
            path = "C:\\Users\\coldc\\Downloads";
            //path = "\\\\CHRISSERVER\\Data\\Etc\\Downloaded items\\furries";
            string[] paths = Directory.GetFiles(path);
            FileList files = new FileList();

            foreach (var jawn in paths)
            {
                FileInfo file = new FileInfo(jawn);
                files.Add(new FileProps() { FileName = file.Name, OriginalPath = file.FullName,
                SizeInBytes = (ulong)file.Length, Status=FileStatus.GOOD, Checksum = new byte[] { 1, 1, 1, 1 }
                });
            }


            List<FileProps> errors;
            List<VolumeProps> sorted = Sorting.SortForEfficientDistribution(files, 4096, 8000000000, true, out errors);

            Console.WriteLine("breakpoint!");
        }

        static void TestDescendingSort()
        {
            FileProps[] props = new FileProps[]
            {
                new FileProps{ SizeInBytes = 430 },
                new FileProps{ SizeInBytes = 600 },
                new FileProps{ SizeInBytes = 2},
                new FileProps{ SizeInBytes = 430},
                new FileProps{ SizeInBytes = 200 },
                new FileProps {SizeInBytes = 9001 },
                new FileProps {SizeInBytes = 573},
                new FileProps {SizeInBytes = 500 },
                new FileProps {SizeInBytes = 1 }
            };

            FileProps[] sorted = Sorting.SortBySizeInBytesDescending(props);

            bool success = true;
            for (int i = 0; i < sorted.Length - 1; i++)
            {
                if (sorted[i].SizeInBytes < sorted[i + 1].SizeInBytes)
                {
                    success = false;
                    break;
                }
            }
            if (!success) Fail("SortBySizeInBytesDescending");
            else Pass("SortBySizeInBytesDescending");

        }

        static void TestFileListOverride()
        {
            VolumeProps test = new VolumeProps(123456789);
            FileProps file = new FileProps { SizeInBytes = 200, OriginalPath = "c:\\asdf\\" };
            ObservableFileList a = new ObservableFileList();
            a.Add(file);
            test.Add(file);
            Console.WriteLine("This should say 200: " + a.TotalSizeInBytes);
        }

        static async Task TestJSONSerializerBigger()
        {
            object _lockObj = new object();
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

            FileAndDiscData data1 = new FileAndDiscData();
            data1.AllFiles.Add(testPropsA);
            data1.AllFiles.Add(testPropsB);
            data1.AllFiles.Add(testPropsC);

            VolumeProps testVol1 = new VolumeProps(100000);
            VolumeProps testVol2 = new VolumeProps(100000);
            testVol1.SetIdentifier(1);
            testVol2.SetIdentifier(2);

            //try adding volumeprops, then changing contents
            data1.AllVolumes.Add(testVol1);
            testVol1.Add(testPropsA);
            testVol1.Add(testPropsB);

            //try changing volumeprops contents, then adding
            testVol2.Add(testPropsB);
            testVol2.Add(testPropsC);
            data1.AllVolumes.Add(testVol2);

            string serialized = JsonSerializer.Serialize(data1);
            FileAndDiscData data2 = JsonSerializer.Deserialize<FileAndDiscData>(serialized);
            data2.PopulateVolumes();
            if (data1 == data2)
            {
                Pass("Deserialization of larger FileAndDiscData");
            }
            else Fail("Deserialization of larger FileAndDiscData");

            List<FileProps> testRemove = (List<FileProps>)await data2.AllFiles.GetFilesByPartialMatch(new FileProps { FileName = "testPropsB", RelatedVolumes = null });
            FileProps removeThis = testRemove.First();
            //bool result = data2.AllFiles.Remove(removeThis);
            ResultCode result = await data2.AllFiles.CascadeRemove(removeThis, data2.AllVolumes, _lockObj);

            VolumeProps comparePropsA = data2.AllVolumes.First();
            VolumeProps comparePropsB = data2.AllVolumes.Last();

            
            if ( !(await comparePropsA.Contains(removeThis)) && !(await comparePropsB.Contains(removeThis)))
            {
                Pass("Cascading remove + reference structure of deserialized FileAndDiscData");
            }
            else Fail("Cascading remove + reference structure of deserialized FileAndDiscData");
        }

        static async void TestJSONSerializer()
        {
            FileAndDiscData data = new FileAndDiscData();
            FileProps testProps = new FileProps
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

            data.AllFiles.Add(testProps);

            VolumeProps testVol = new VolumeProps(100000);
            data.AllVolumes.Add(testVol);
            testVol.Add(data.AllFiles.First());

            Console.WriteLine("Testing serialization of FileProps");
            string serializeProps = JsonSerializer.Serialize(testProps);
            FileProps deserializeProps = JsonSerializer.Deserialize<FileProps>(serializeProps);
            if (deserializeProps == testProps)
            {
                Pass("Deserialized FileProps is equal to the original FileProps");
            }
            else Fail("Deserialized FileProps is not equal to the original FileProps");

            Console.WriteLine("Verifying data correctness before serialization:");
            if (data.AllVolumes.First().Files.First().RelatedVolumes.First().VolumeID == testVol.Identifier)
            {
                Pass("Volume ID present in file matches the ID of the volume");
            }
            else Fail("Volume ID present in file matches the ID of the volume");

            string serialized = JsonSerializer.Serialize(data);

            FileAndDiscData deserialized = JsonSerializer.Deserialize<FileAndDiscData>(serialized);
            deserialized.PopulateVolumes();

            DiscAndBurnStatus test1 = data.AllFiles.First().RelatedVolumes.First();
            DiscAndBurnStatus test2 = deserialized.AllFiles.First().RelatedVolumes.First();
            if (test1 == test2) Pass("Directly compare RelatedVolumes structs (no enumeration)");
            else Fail("Directly compare RelatedVolumes structs (no enumeration)");

            VolumeProps testVol1 = data.AllVolumes.First();
            VolumeProps testVol2 = deserialized.AllVolumes.First();
            if (testVol1 == testVol2)
            {
                Pass("Compare first VolumeProps to first deserialized VolumeProps");
            }
            else Fail("Compare first VolumeProps to first deserialized VolumeProps");

            FileProps testfile1 = data.AllVolumes.First().Files.First();
            FileProps testfile2 = deserialized.AllFiles.First();

            if (testfile1 == testfile2)
            {
                Pass("Compare data content of test file in data.AllVolumes to test file in deserialized.AllFiles");
            }
            else Fail("Compare data content of test file in data.AllVolumes to test file in deserialized.AllFiles");

            if (Object.ReferenceEquals(deserialized.AllFiles.First(), deserialized.AllVolumes.First().Files.First()))
            {
                Pass("Restructure of object references after deserialization");
            }
            else Fail("Restructure of object references after deserialization");

            if (data == deserialized)
            {
                Pass("Comparison of entire FileAndDiscData struct");
            }
            else Fail("Comparison of entire FileAndDiscData struct");
        }

        static async void TestAPI()
        {
            BurnManagerAPI testData = new BurnManagerAPI();
            await testData.TestState();

            ObservableFileAndDiscData compare = new ObservableFileAndDiscData(testData.Data);

            if (compare == testData.Data)
            {
                Pass("Comparison of copied FileAndDiscData to original FileAndDiscData.");
            }
            else Fail("Comparison of copied FileAndDiscData to original FileAndDiscData.");

            string jsonString = testData.Serialize();
            ResultCode result = 0;
            ObservableFileAndDiscData deserialized = new ObservableFileAndDiscData(BurnManagerAPI.JsonToFileAndDiscData(jsonString, ref result));
            deserialized.PopulateVolumes();

            if (deserialized == testData.Data && result == ResultCode.SUCCESSFUL)
            {
                Pass("Deserialize & reserialize");
            }
            else Fail("Deserialize & reserialize");
        }

        static async void TestDataTypes()
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
            FileProps incompleteProps = new FileProps
            {
                FileName = "testPropsB"
            };

            if (FileProps.PartialEquals(testPropsB, incompleteProps))
            {
                Pass("FileProps.PartialEquals true condition");
            }
            else
            {
                Fail("FileProps.PartialEquals true condition");
            }
            if (!FileProps.PartialEquals(testPropsA, incompleteProps))
            {
                Pass("FileProps.PartialEquals false condition");
            }
            else
            {
                Fail("FileProps.PartialEquals false condition");
            }


            FileProps testPropsACopy = new FileProps(testPropsA);
            if (testPropsA == testPropsACopy)
            {
                Pass("FileProps equality true condition");
                Pass("FileProps copy constructor");
            }
            else
            {
                Fail("FileProps equality true condition");
                Fail("FileProps copy constructor");
            }

            if (testPropsA != testPropsB)
            {
                Pass("FileProps equality false condition");
            }
            else Fail("FileProps equality false condition");

            //Note: Test FileProps copy constructor again after adding one to a VolumeProps (RelatedVolumes & Monolithic structs should be ==)

            VolumeProps volPropsA = new VolumeProps(100000000);
            VolumeProps volPropsB = new VolumeProps(987654321);
            volPropsA.Add(testPropsA); //500bytes
            volPropsA.Add(testPropsB); //300bytes

            if (volPropsA.SpaceRemaining == (100000000 - 800)) Pass("VolumeProps space remaining decrement");
            else Fail("VolumeProps space remaining decrement: " + volPropsA.SpaceRemaining);

            if (volPropsA.SpaceUsed == 800) Pass("VolumeProps space used increment.");
            else Fail("VolumeProps space used increment: " + volPropsA.SpaceUsed);

            if (testPropsA.RelatedVolumes[0].VolumeID == volPropsA.Identifier) Pass("Adding volume to FileProps on VolumeProps add, and VolumeProps equality.");
            else Fail("Adding volume to FileProps on VolumeProps add, and VolumeProps equality.");

            //if (testPropsA.RelatedVolumes[0].VolumeID == volPropsB.Identifier) Fail("VolumeProps equality false.");
            //else Pass("VolumeProps equality false.");

            if (await volPropsA.Contains(testPropsA) && !(await volPropsA.Contains(testPropsC))){
                Pass("VolumeProps contains");
            }
            else Fail("VolumeProps contains");

            FileProps testPropsBCopy = new FileProps(testPropsB);
            if (testPropsB == testPropsBCopy) Pass("FileProps copy 1 & equality");
            else Fail("FileProps copy 1 & equality");
            if (CollectionComparers.CompareLists(testPropsB.RelatedVolumes, testPropsBCopy.RelatedVolumes))
            {
                Pass("FileProps copy 2: DiscAndBurnStatus copy & compare");
            }
            else Fail("FileProps copy 2: DiscAndBurnStatus copy & compare");
            if (testPropsB.RelatedVolumes[0] == testPropsBCopy.RelatedVolumes[0]) Pass("Compare 1st element of DiscAndBurnStatus to copied DiscAndBurnStatus");
            else Fail("Compare 1st element of DiscAndBurnStatus to copied DiscAndBurnStatus");
            if (testPropsB.RelatedVolumes[0].VolumeID == testPropsBCopy.RelatedVolumes[0].VolumeID)
            {
                Pass("DiscAndBurnStatus element Volume property to copied DiscAndBurnStatus Volume property equality, comparing two VolumeProps");
            }
            else Fail("DiscAndBurnStatus element Volume property to copied DiscAndBurnStatus Volume property equality, comparing two VolumeProps");

            volPropsA.CascadeRemove(testPropsA);
            if (volPropsA.SpaceRemaining == (100000000 - 300) && volPropsA.SpaceUsed == 300) Pass("VolumeProps space remaining/used increment/decrement");
            else Fail("VolumeProps space remaining/used increment/decrement");

            if (testPropsA.RelatedVolumes.Count == 0) Pass("RelatedVolumes decrement with removal of FileProps from VolumeProps");
            else Fail("RelatedVolumes decrement with removal of FileProps from VolumeProps");

            volPropsA.Add(testPropsA);
            volPropsA.AssignIdentifierDelegate(DummyIDDelegate);
            volPropsA.SetIdentifier();

            if (volPropsA.Identifier == 5) Pass("VolumeProps identifier delegate set and assignment");
            else Fail("VolumeProps identifier delegate set and assignment");

            VolumeProps volPropsACopy = new VolumeProps(volPropsA);
            volPropsACopy.SetIdentifier();
            if (volPropsA == volPropsACopy) Pass("VolumeProps copy constructor 1 & equality");
            else Fail("VolumeProps copy constructor 1 & equality");
            if (await volPropsACopy.Contains(testPropsA)) Pass("VolumeProps copy constructor 2 & Contains");
            else Fail("VolumeProps copy constructor 2 & Contains");

            volPropsB.Add(testPropsA);
            int foundVols = 0;
            foreach (var file in testPropsA.GetPendingBurns())
            {
                foundVols++;
            }
            if (foundVols == 2) Pass("FileProps.GetPendingBurns()");
            else Fail("FileProps.GetPendingBurns()");

            FileAndDiscData allData = new FileAndDiscData();
            allData.AllFiles.Add(testPropsA);
            allData.AllFiles.Add(testPropsB);
            allData.AllFiles.Add(testPropsC);
            allData.AllVolumes.Add(volPropsA);
            allData.AllVolumes.Add(volPropsB);

            FileAndDiscData noData = new FileAndDiscData();

            if (allData != noData) Pass("FileAndDiscData equality test false");
            else Fail("FileAndDiscData equality test false");

            FileAndDiscData allDataCopy = new FileAndDiscData(allData);
            if (allData == allDataCopy) Pass("FileAndDiscData copy test 1 & equality");
            else Fail("FileAndDiscData copy test 1 & equality");

            if (allData.AllFiles == allDataCopy.AllFiles) Pass("FileAndDiscData copy test 2 and FileList equality");
            else Fail("FileAndDiscData copy test 2");

            FileList testList = new FileList();
            testList.Add(testPropsB);
            testList.Add(testPropsC);

            if (testList != allData.AllFiles) Pass("FileList inequality");
            else Fail("FileList inequality");

            FileList compareList = new FileList();
            compareList.Add(testPropsA);
            compareList.Add(testPropsB);

            if (testList != compareList) Pass("FileList inequality 2 (equal length lists with different contents)");
            else Fail("FileList inequality 2 (equal length lists with different contents)");
        }

        public static int DummyIDDelegate()
        {
            return 5;
        }

        static void Pass(string description)
        {
            Console.WriteLine("Passed: " + description);
        }
        static void Fail(string description)
        {
            Console.WriteLine("FAILED: " + description);
        }


    }

}