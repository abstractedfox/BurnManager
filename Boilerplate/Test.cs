//Boilerplate main for testing purposes.

using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace BurnManager
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            TestDataTypes();
            while (true) ; //prevent returning from main when awaited calls cause flow control to continue here
        }

        static async void TestDataTypes()
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
            await volPropsA.Add(testPropsA); //500bytes
            await volPropsA.Add(testPropsB); //300bytes

            if (volPropsA.SpaceRemaining == (100000000 - 800)) Pass("VolumeProps space remaining decrement");
            else Fail("VolumeProps space remaining decrement: " + volPropsA.SpaceRemaining);

            if (volPropsA.SpaceUsed == 800) Pass("VolumeProps space used increment.");
            else Fail("VolumeProps space used increment: " + volPropsA.SpaceUsed);

            if (testPropsA.RelatedVolumes[0].Volume == volPropsA) Pass("Adding volume to FileProps on VolumeProps add, and VolumeProps equality.");
            else Fail("Adding volume to FileProps on VolumeProps add, and VolumeProps equality.");

            if (testPropsA.RelatedVolumes[0].Volume == volPropsB) Fail("VolumeProps inequality.");
            else Pass("VolumeProps inequality.");

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
            if (testPropsB.RelatedVolumes[0].Volume == testPropsBCopy.RelatedVolumes[0].Volume)
            {
                Pass("DiscAndBurnStatus element Volume property to copied DiscAndBurnStatus Volume property equality, comparing two VolumeProps");
            }
            else Fail("DiscAndBurnStatus element Volume property to copied DiscAndBurnStatus Volume property equality, comparing two VolumeProps");

            await volPropsA.Remove(testPropsA);
            if (volPropsA.SpaceRemaining == (100000000 - 300) && volPropsA.SpaceUsed == 300) Pass("VolumeProps space remaining/used increment/decrement");
            else Fail("VolumeProps space remaining/used increment/decrement");

            if (testPropsA.RelatedVolumes.Count == 0) Pass("RelatedVolumes decrement with removal of FileProps from VolumeProps");
            else Fail("RelatedVolumes decrement with removal of FileProps from VolumeProps");

            await volPropsA.Add(testPropsA);
            volPropsA.AssignIdentifierDelegate(DummyIDDelegate);
            volPropsA.SetIdentifier();

            if (volPropsA.Identifier == "asdf") Pass("VolumeProps identifier delegate set and assignment");
            else Fail("VolumeProps identifier delegate set and assignment");

            VolumeProps volPropsACopy = new VolumeProps(volPropsA);
            volPropsACopy.SetIdentifier();
            if (volPropsA == volPropsACopy) Pass("VolumeProps copy constructor 1 & equality");
            else Fail("VolumeProps copy constructor 1 & equality");
            if (await volPropsACopy.Contains(testPropsA)) Pass("VolumeProps copy constructor 2 & Contains");
            else Fail("VolumeProps copy constructor 2 & Contains");

            await volPropsB.Add(testPropsA);
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

            if (CollectionComparers.CompareLists(allData.AllVolumes, allDataCopy.AllVolumes)) Pass("FileAndDiscData copy test 3");
            else Fail("FileAndDiscData copy test 3");

            FileList testList = new FileList();
            testList.Add(testPropsB);
            testList.Add(testPropsC);

            if (testList != allData.AllFiles) Pass("FileList inequality");
            else Fail("FileList inequality");
        }

        public static string DummyIDDelegate()
        {
            return "asdf";
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