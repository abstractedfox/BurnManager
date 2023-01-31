using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    //Most runtime program logic interfaces through this class
    public class BurnManagerAPI
    {
        public FileAndDiscData data;
        public FileAndDiscData lastSavedState;

        public BurnManagerAPI()
        {
            data = new FileAndDiscData();
            lastSavedState = data;
        }

        public void Serialize()
        {

        }
        public void Deserialize()
        {

        }


        //===================File and sorting operations
        public void AddFile()
        {

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

        public static void GenerateChecksums(FileList files)
        {

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
            VolumeProps volPropsA = new VolumeProps(100000000);
            VolumeProps volPropsB = new VolumeProps(987654321);
            await volPropsA.Add(testPropsA); //500bytes
            await volPropsA.Add(testPropsB); //300bytes
            await volPropsB.Add(testPropsC);

            data.AllFiles.Add(testPropsA);
            data.AllFiles.Add(testPropsB);
            data.AllFiles.Add(testPropsC);
            data.AllVolumes.Add(volPropsA);
            data.AllVolumes.Add(volPropsB);

        }
    }
}
