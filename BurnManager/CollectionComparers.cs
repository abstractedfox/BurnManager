//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

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

            foreach (var itemA in listA)
            {
                bool match = false;
                foreach (var itemB in listB)
                {
                    if (itemA == itemB) match = true;
                    break;
                }
                if (!match)
                {
                    return false;
                }
            }
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

        public static bool CompareFileLists(FileList a, FileList b)
        {
            int aCount = a.Count;
            int bCount = b.Count;
            if (aCount + bCount == 0) return true;
            if (aCount != bCount) return false;

            foreach (var item in a)
            {
                if (!b.Contains(item)) return false;
            }
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
