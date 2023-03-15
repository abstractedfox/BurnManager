using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BurnManager
{
    public class Sorting
    {
        //Distributes files by populating each volume with the biggest file that will fit,
        //then continues by adding each next file that is closest in size to half the remaining space
        public static List<VolumeProps> SortForEfficientDistribution(ICollection<FileProps> files, ulong clusterSize, 
            ulong volumeSize, out List<FileProps> erroredFiles)
        {
            LinkedList<FileProps> sortedFiles = new LinkedList<FileProps>(SortBySizeInBytesDescending(files));
            erroredFiles = new List<FileProps>();
            List<VolumeProps> results = new List<VolumeProps>();

            //Remove any files too big for the volume size
            while (sortedFiles.First != null && sortedFiles.First.Value.SizeInBytes > volumeSize)
            {
                erroredFiles.Add(sortedFiles.First.Value);
                sortedFiles.RemoveFirst();
            }

            if (sortedFiles.Count == 0) return new List<VolumeProps>();

            LinkedListNode<FileProps> origin = sortedFiles.First;

            while (sortedFiles.Count > 0)
            {
                VolumeProps volume = new VolumeProps(volumeSize);
                volume.SetIdentifier(VolumeProps.GetNewID(results));
                volume.Name = "Sorted Volume " + volume.Identifier;

                volume.ClusterSize = clusterSize;
                volume.Add(sortedFiles.First.Value);
                sortedFiles.RemoveFirst();

                origin = sortedFiles.First;

                while (sortedFiles.Count > 0 &&
                    volume.ClusterSizeAdjustment((ulong)sortedFiles.Last.Value.SizeInBytes) <= volume.SpaceRemaining)
                {

                    ulong targetSize = (ulong)volume.SpaceRemaining / 2;
                    //LinkedListNode<FileProps> origin = sortedFiles.First;

                    LinkedListNode<FileProps> nextFile = _findNearestNodeToTargetSize(origin, targetSize);


                    bool outOfSpace = false;
                    while (volume.ClusterSizeAdjustment((ulong)nextFile.Value.SizeInBytes) > volume.SpaceRemaining)
                    {
                        if (nextFile.Next == null)
                        {
                            outOfSpace = true;
                            break;
                        }
                        nextFile = nextFile.Next;
                    }
                    if (outOfSpace) break;

                    volume.Add(nextFile.Value);

                    if (nextFile.Next != null) origin = nextFile.Next;
                    else if (nextFile.Previous != null) origin = nextFile.Previous;

                    sortedFiles.Remove(nextFile);
                }


                results.Add(volume);
            }

            return results;
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
                ulong compareDifference = _ulongAbsoluteDifference(targetSize, (ulong)currentNode.Previous.Value.SizeInBytes);
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
                ulong compareDifference = _ulongAbsoluteDifference(targetSize, (ulong)currentNode.Next.Value.SizeInBytes);
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
