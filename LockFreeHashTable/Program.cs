using System;
using System.Collections.Generic;
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
            LFHashTable lfht = new LFHashTable();
            Random rand = new Random();
            Task task = new Task(() =>
            {
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));


            });
            Task task2 = new Task(() =>
            {
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));
                lfht.Insert(rand.Next(30));


            });

            task.Start();
            task2.Start();

            Console.WriteLine("Hello World!");
            Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 
        }
    }
}
