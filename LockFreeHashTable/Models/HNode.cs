using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LockFreeHashTable.Models
{
    public class HNode
    {
        public FSet[] Buckets;
        public int Size;
        public HNode Pred;

        public HNode(int size, HNode pred)
        {
            Buckets = new FSet[size];
            Size = size;
            Pred = pred;
        }
    }
}
