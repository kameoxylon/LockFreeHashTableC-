using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LockFreeHashTable.Models;

namespace LockFreeHashTable
{
    class Program
    {
        static void Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            TestsOne();
            TestTwo();
            TestThree();
            Console.WriteLine("Hello World!");
            Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }

        //500,000 operations on 50% insert, 50% contains
        public static void TestsOne()
        {
            Console.WriteLine("50% insert, 50% contains");
            Benchmark(.5, 0, .5, 5000000, 1);
            Benchmark(.5, 0, .5, 2500000, 2);
            Benchmark(.5, 0, .5, 1250000, 4);
            Benchmark(.5, 0, .5, 625000, 8);

        }

        //500,000 operations on 50% insert, 50% remove
        public static void TestTwo()
        {
            Console.WriteLine("50% insert, 50% remove");
            Benchmark(.5, .5, 0, 5000000, 1);
            Benchmark(.5, .5, 0, 2500000, 2);
            Benchmark(.5, .5, 0, 1250000, 4);
            Benchmark(.5, .5, 0, 625000, 8);
        }

        //500,000 operations on 50% insert, 30% remove, 20% contains
        public static void TestThree()
        {
            Console.WriteLine("50% insert, 30% remove, 20% contains");
            Benchmark(.5, .3, .2, 5000000, 1);
            Benchmark(.5, .3, .2, 2500000, 2);
            Benchmark(.5, .3, .2, 1250000, 4);
            Benchmark(.5, .3, .2, 625000, 8);
        }

        public static void Benchmark(double insert, double remove, double contains, int size, int numOfThreads)
        {
            LFHashTable lfht = new LFHashTable();
            List<Task> tasks = new List<Task>();
            for(int i = 0; i < numOfThreads; i++)
            {
                Queue<int> allNums = new Queue<int>();
                Queue<int> allOps = CreateOps(insert, remove, contains, allNums, size);

                Task task = new Task(() => Start(allOps, allNums, lfht));
                tasks.Add(task);
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (Task task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("Number of threads: " + numOfThreads + " runtime: " + elapsedTime + "s");
        }

        public static void Start(Queue<int> allOps, Queue<int> nums, LFHashTable lfht)
        {
            while (allOps.Count() > 0)
            {
                int op = allOps.Dequeue();
                switch (op)
                {
                    case -1:
                        lfht.Remove(nums.Dequeue());
                        break;
                    case 0:
                        lfht.Contains(nums.Dequeue());
                        break;
                    case 1:
                        lfht.Insert(nums.Dequeue());
                        break;
                }
            }
        }

        public static Queue<int> CreateOps(double insert, double remove, double contains, Queue<int> num, int size)
        {
            List<int> list = new List<int>();
            Random rng = new Random();

            int insertCount = (int)(insert * size), removeCount = (int)(remove * size), containsCount = (int)(contains * size);
            while(insertCount > 0)
            {
                list.Add(1);
                num.Enqueue(rng.Next(1, 10000));
                insertCount--;
            }
            while (removeCount > 0)
            {
                list.Add(-1);
                num.Enqueue(rng.Next(1, 10000));
                removeCount--;
            }
            while (containsCount > 0)
            {
                list.Add(0);
                num.Enqueue(rng.Next(1, 10000));
                containsCount--;
            }


            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            Queue<int> queue = new Queue<int>();
            foreach(int i in list)
            {
                queue.Enqueue(i);
            }

            return queue;
        }
    }
}
