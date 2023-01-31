using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class CollectionComparers
    {
        //Compare the contents (but not the order) of two lists. Not intended for nested collections.
        public static bool CompareLists(IList listA, IList listB)
        {
            int aCount = listA.Count;
            int bCount = listB.Count;
            if (aCount + bCount == 0) return true;
            if (aCount != bCount) return false;
            foreach (var item in listA) if (!listB.Contains(item)) return false;
            return true;
        }

        public static bool CompareCollections(ICollection<object> collectionA, ICollection<object> collectionB)
        {
            int aCount = collectionA.Count;
            int bCount = collectionB.Count;
            if (aCount + bCount == 0) return true;
            if (aCount != bCount) return false;

            foreach (var item in collectionA) if (!collectionB.Contains(item)) return false;
            return true;
        }

        public static bool CompareByteArrays(byte[] arrayA, byte[]arrayB)
        {
            if (arrayA.Length != arrayB.Length) return false;
            for (int i = 0; i < arrayA.Length; i++)
            {
                if (arrayA[i] != arrayB[i]) return false;
            }
            return true;
        }
    }
}
