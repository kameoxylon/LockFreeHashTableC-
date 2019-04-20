using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LockFreeHashTable.Models
{
    public class FSet
    {
        public List<int> Set;
        public bool Ok;

        public FSet(int newVar, bool ok)
        {
            Set = new List<int>()
            {
                newVar
            };
            Ok = ok;
        }

        public FSet()
        {
            Set = new List<int>();
            Ok = false;
        }

        public FSet(List<int> set, bool ok)
        {
            Set = set;
            Ok = ok;
        }
    }
}
