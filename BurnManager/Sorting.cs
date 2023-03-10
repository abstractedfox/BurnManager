using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class Sorting
    {
        public static List<VolumeProps> SortForEfficientDistribution(ICollection<FileProps> files, int clusterSize, 
            ulong volumeSize, out List<FileProps> erroredFiles)
        {
            //List<FileProps> sortedFiles = new List<FileProps>(SortBySizeInBytesDescending(files));
            LinkedList<FileProps> sortedFiles = new LinkedList<FileProps>(SortBySizeInBytesDescending(files));
            erroredFiles = new List<FileProps>();

            //Remove any files too big for the volume size
            while (sortedFiles.First.Value.SizeInBytes > volumeSize)
            {
                erroredFiles.Add(sortedFiles.First.Value);
                sortedFiles.RemoveFirst();
            }

            while (sortedFiles.Count > 0)
            {
                VolumeProps volume = new VolumeProps(volumeSize);
                volume.Add(sortedFiles.First.Value);
                sortedFiles.RemoveFirst();


            }
        }

        public static FileProps[] SortBySizeInBytesDescending(ICollection<FileProps> files)
        {
            FileProps[] sorted = new FileProps[files.Count];
            int position = 0;
            foreach (var file in files)
            {
                sorted[position] = file;
                position++;
            }

            for (int i = 1; i < sorted.Length; i++)
            {
                int currentPos = i;
                bool reeval = false;
                while (currentPos > 0 && sorted[currentPos - 1].SizeInBytes < sorted[currentPos].SizeInBytes)
                {
                    FileProps hold = sorted[currentPos - 1];
                    sorted[currentPos - 1] = sorted[currentPos];
                    sorted[currentPos] = hold;
                    currentPos--;
                    reeval = true;
                }
                if (reeval)
                {
                    i--;
                }
            }

            return sorted;
        }

        //From a LinkedListNode, traverse the list and find the node whose SizeInBytes
        //is closest to the value passed to 'targetSize'. If the next largest and next smallest
        //nodes are different from targetSize by the same number of bytes, it returns the smaller node
        private static LinkedListNode<FileProps>? _findNearestNodeToTargetSize(LinkedListNode<FileProps> origin, ulong targetSize)
        {
            var currentNode = origin;
            LinkedListNode<FileProps> favorite = origin;
            ulong favoriteDifference = _ulongAbsoluteDifference((ulong)favorite.Value.SizeInBytes, targetSize);

            //traverse toward 0
            while (currentNode.Previous != null)
            {
                ulong compareDifference = _ulongAbsoluteDifference(targetSize, (ulong)favorite.Previous.Value.SizeInBytes);
                if (compareDifference <= favoriteDifference)
                {
                    currentNode = currentNode.Previous;
                    favorite = currentNode;
                    favoriteDifference = compareDifference;
                }
                else break;
            }

            //traverse away from 0
            currentNode = origin;
            while (currentNode.Next != null)
            {
                ulong compareDifference = _ulongAbsoluteDifference(targetSize, (ulong)favorite.Next.Value.SizeInBytes);
                if (compareDifference <= favoriteDifference)
                {
                    currentNode = currentNode.Next;
                    favorite = currentNode;
                    favoriteDifference = compareDifference;
                }
                else break;
            }

            //note: the above covers cases where the origin node may be surrounded by nodes with
            //identical SizeInBytes, where the origin may be at the beginning or end, and where
            //the origin is the only node, and uses no extra logic to find the smaller node
            //where both the next next larger and next smaller nodes are equally different from the target value

            return favorite;
        }

        private static ulong _ulongAbsoluteDifference(ulong a, ulong b)
        {
            if (a == b) return 0;
            if (a > b) return a - b;
            return b - a;
        }
    }
}
