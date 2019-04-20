using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LockFreeHashTable.Models
{
    public class FSetOp
    {
        public int OpType;
        public int Key;
        public bool Done;
        public bool Resp;

        public FSetOp(int opType, int key)
        {
            Done = false;
            Key = key;
            Resp = false;
            OpType = opType;
        }
    }
}
