using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LockFreeHashTable.Models
{
    public class LFHashTable
    {
        public FSet FSet;
        public FSetOp FSetOp;
        public HNode Head;
        public int NumberOfElements;
        public int Size;

        public LFHashTable()
        {
            Head = new HNode(1, null);
            Head.Buckets[0] = new FSet(0, true);
            NumberOfElements = 0;
            Size = 1;
        }

        public bool Insert(int k)
        {
            bool resp = Apply(1, k);
            if(NumberOfElements > (Size / 2))
            {
                Resize(true);
            }

            NumberOfElements++;
            return resp;
        }

        public bool Remove(int k)
        {
            bool resp = Apply(-1, k);
            if(NumberOfElements > (Size / 2))
            {
                Resize(true);
            }

            NumberOfElements++;
            return resp;
        }

        public bool Contains(int k)
        {
            HNode temp = Head;
            FSet b = temp.Buckets[k % temp.Size];
            if(b == null)
            {
                HNode s = temp.Pred;
                if(s != null)
                {
                    b = s.Buckets[k % s.Size];
                }
                else
                {
                    b = temp.Buckets[k % temp.Size];
                }
            }
            return HasMember(b, k);
        }

        public void Resize(bool grow)
        {
            HNode temp = Head;
            if(temp.Size > 1 || grow)
            {
                for(int i = 0; i < temp.Size; i++)
                {
                    InitBucket(temp, i);
                }
                temp.Pred = null;
                int size;

                if(grow)
                {
                    size = temp.Size * 2;
                }
                else
                {
                    size = temp.Size / 2;
                }

                HNode tempPrime = new HNode(size, temp);
                Interlocked.CompareExchange(ref Head, tempPrime, temp);
                Size = size;
            }
        }

        public bool Apply(int typeOp, int k)
        {
            FSetOp fSetOp = new FSetOp(typeOp, k);
            while(true)
            {
                HNode temp = Head;
                FSet b = temp.Buckets[k % temp.Size];
                if(b == null)
                {
                    b = InitBucket(temp, k % temp.Size);
                }
                if(Invoke(b, fSetOp))
                {
                    return GetResponse(fSetOp);
                }
            }
        }

        public FSet InitBucket(HNode temp, int i)
        {
            FSet b = temp.Buckets[i];
            HNode s = temp.Pred;
            List<int> mySet;
            if(b == null && s != null)
            {
                if(temp.Size == (s.Size * 2))
                {
                    FSet m = s.Buckets[i % s.Size];
                    List<int> mSet = Freeze(m);
                    mySet = Intersect(mSet, temp.Size, i);
                    UnFreeze(m);
                }
                else
                {
                    FSet m = s.Buckets[i];
                    FSet n = s.Buckets[i + temp.Size];
                    List<int> mSet = Freeze(m);
                    List<int> nSet = Freeze(n);
                    mySet = Union(mSet, nSet);
                    UnFreeze(m);
                    UnFreeze(n);
                }

                FSet bPrime = new FSet(mySet, true);
                Interlocked.CompareExchange(ref temp.Buckets[i], bPrime, null);
            }
            return temp.Buckets[i];
        }

        public bool GetResponse(FSetOp fSetOp)
        {
            return fSetOp.Resp;
        }

        public bool HasMember(FSet b, int k)
        {
            if(b.Set.Any(x => x == k))
            {
                return true;
            }
            return false;
        }

        public bool Invoke(FSet b, FSetOp op)
        {
            if(b.Ok && !op.Done)
            {
                if(op.OpType == 1)
                {
                    op.Resp = !HasMember(b, op.Key);
                    b.Set.Add(op.Key);
                }
                else if(op.OpType == -1)
                {
                    op.Resp = HasMember(b, op.Key);
                    b.Set.Remove(op.Key);
                }
                op.Done = true;
            }
            return op.Done;
        }

        public List<int> Freeze (FSet b)
        {
            if(b.Ok)
            {
                b.Ok = false;
            }
            return b.Set;
        }

        public List<int> UnFreeze (FSet b)
        {
            if(!b.Ok)
            {
                b.Ok = true;
            }
            return b.Set;
        }

        public List<int> Intersect(List<int> b, int size, int moddedNum)
        {
            List<int> nSet = new List<int>();
            foreach(int i in b)
            {
                if(i % size == moddedNum)
                {
                    nSet.Add(i);
                }
            }
            return nSet;
        }

        public List<int> Union(List<int> a, List<int> b)
        {
            List<int> mySet = a.Union(b).ToList();
            return mySet;

        }
    }
}
