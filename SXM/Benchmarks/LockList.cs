/*
 IMPORTANT:  READ BEFORE DOWNLOADING, COPYING, INSTALLING OR USING.
 By downloading, copying, installing or using the software you agree to this license.
 If you do not agree to this license, do not download, install, copy or use the software.

 Brown University License Agreement
 Copyright (c) 2003, Brown University 
 All rights reserved.

 Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: 
  •	Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
  •	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
  •	The name of Brown University may not be used to endorse or promote products derived from this software without specific prior written permission. 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Threading;

namespace SXM
{
	/// <summary>
	/// Summary description for XIntSetBenchMark.
	/// </summary>
	public class LockList : Benchmark
	{
		const int INITIAL_SIZE = 1024;

		// statistics
		int insertCalls;
		int removeCalls;
		int containsCalls;
		int delta;

		protected List first;

		///<remark>
		/// Initializes the list shared by all the threads.
		/// </remark>
		public LockList() 
		{
			int size = 2;
			this.first = new List(Int32.MinValue);
			this.first.next   = new List(Int32.MaxValue);
			Random random = new Random(this.GetHashCode());
			while (size < INITIAL_SIZE) 
			{
				int v = random.Next();
				List newList = new List(v);
				List prevList = this.first;
				List currList = prevList.Next;
				while (currList.value < v) 
				{
					prevList = currList;
					currList = currList.Next;
				}
				if (currList.Value != v) 
				{
					size++;
					newList.Next = prevList.Next;
					prevList.Next = newList;
				}
			}
		}


		/// <summary>
		/// This method does most of the work. It finds the list entries containing
		/// the value, or, if the value is absent, the closest entries.
		/// </summary>
		/// <param name="v">Search for this value</param>
		/// <returns>Structure containing closest list entries.</returns>
		protected Neighborhood Find(int v) 
		{
			List prevList = this.first;
			List currList = prevList.Next;
			while (currList.Value < v) 
			{
				prevList = currList;
				currList = prevList.Next;
			}
			if (currList.Value == v)
				return new Neighborhood(prevList, currList);
			else
				return new Neighborhood(prevList);
		}

		/// <summary>
		/// Add a value to the set.
		/// </summary>
		/// <param name="v">Value to add.</param>
		/// <returns>Whether that value was not already there.</returns>
		public bool Insert(int v) 
		{
			lock (this) 
			{
				Common.commits++;
				List newList = new List(v);

				Neighborhood hood = Find(v);
				if (hood.currList != null) 
				{
					return false;
				} 
				else 
				{
					List prevList = hood.prevList;
					newList.Next = prevList.Next;
					prevList.Next = newList;
					return true;
				}
			}
		}
  
		/// <summary>
		/// Test whether a value is in a set.
		/// </summary>
		/// <param name="v">Value to test.</param>
		/// <returns>Whether that value is present.</returns>
		public bool Contains(int v) 
		{
			lock (this) 
			{
				Common.commits++;
				bool result = true;
				Neighborhood hood = Find(v);
				result = (hood.currList != null);
				return result;
			}
		}
  

		/// <summary>
		/// Remove a value from the set.
		/// </summary>
		/// <param name="v">Value to remove.</param>
		/// <returns>Whether that value was not already there.</returns>
		public bool Remove(int v) 
		{
			lock (this) 
			{
				Common.commits++;
				List newList = new List(v);
				bool result = true;
				Neighborhood hood = Find(v);
				if (hood.currList == null) 
				{
					result = false;
				} 
				else 
				{
					List prevList = hood.prevList;
					prevList.Next = hood.currList.Next;
					result = true;
				}
				return result;
			}
		}

		public class Neighborhood 
		{
			public List prevList;
			public List currList;
			public Neighborhood(List prevList, List currList) 
			{
				this.prevList = prevList;
				this.currList = currList;
			}
			public Neighborhood(List prevList) 
			{
				this.prevList = prevList;
			}
		}

		/// <summary>
		/// Method run by each thread
		/// </summary>
		public override void Run() 
		{
			Random random = new Random(this.GetHashCode());
			int myInsertCalls = 0;
			int myRemoveCalls = 0;
			int myContainsCalls = 0;
			int myDelta = 0;

			bool toggle = true;
			int value = 0;
			while (!Common.stop)	// will be interrupted by GracefulException
			{
				int element = random.Next();
				if (Math.Abs(element) % 100 < experiment) 
				{
					if (toggle) 
					{        // insert on even turns
						myInsertCalls++;
						value = element / 100;
						if (Insert(value))
							myDelta++;
					} 
					else 
					{         // remove on odd turns
						myRemoveCalls++;
						if (Remove(value))
							myDelta--;
					}
					toggle = !toggle;
				} 
				else 
				{
					Contains(element / 100);
					myContainsCalls++;
				}
			}
			// update statistics
			UpdateStatistic(ref insertCalls, myInsertCalls);
			UpdateStatistic(ref removeCalls, myRemoveCalls);
			UpdateStatistic(ref containsCalls, myContainsCalls);
			UpdateStatistic(ref delta, myDelta);

		}

		/// <summary>
		///  Atomically update statistic at end of run.
		/// </summary>
		/// <param name="stat">ref to statistic</param>
		/// <param name="value">add this value</param>
		private void UpdateStatistic(ref int stat, int value) 
		{
			int oldValue;
			do 
			{
				oldValue = stat;
			} while (Interlocked.CompareExchange(
				ref stat,
				oldValue + value,
				oldValue) != oldValue);
		}

		public override void Report() 
		{
			Console.WriteLine("Calls:");
			Console.WriteLine("\tInsert:\t\t{0}", insertCalls);
			Console.WriteLine("\tRemove:\t\t{0}", removeCalls);
			Console.WriteLine("\tContains:\t{0}", containsCalls);
		}

		public override void Validate() 
		{
			int expected = INITIAL_SIZE + delta;
			int length = 1;
			List list = this.first;

			int prevValue = list.value;
			bool sorted = true;
			while(list.next != null) 
			{
				list = list.Next;
				length++;
				if (list.Value < prevValue && sorted) 
				{
					ReportError("List not sorted");
					sorted = false;
				}
				if (list.Value == prevValue && sorted) 
				{
					ReportError("Duplicates!!");
					sorted = false;
				}
				prevValue = list.value;
			}
			if (length != expected)
				ReportError("Bad size: " + length +
					" expected " + expected +
					" delta: " + delta);
			else if (sorted)
				Console.WriteLine("List OK");
		}

		public class List: ICloneable 
		{
			public int value;
			public List next;
    
			public List(int v) 
			{
				this.value = v;
			}
    
			public virtual int Value
			{
				get
				{
					return value;
				}
				set
				{
					this.value = value;
				}
			}
			public virtual List Next
			{
				get
				{
					return next;
				}
				set
				{
					this.next = value;
				}
			}
			public object Clone() 
			{
				List newList = new List(this.value);
				newList.next = this.next;
				return newList;
			}    
		}

		protected static void ReportError(String s) 
		{
			Console.WriteLine("ERROR: " + s);
		}
	}
}
