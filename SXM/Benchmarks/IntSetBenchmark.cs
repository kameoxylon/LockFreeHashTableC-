using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Summary description for XIntSetBenchMark.
	/// </summary>
	public abstract class IntSetBenchmark : Benchmark
	{

		#region fields
		public const int INITIAL_SIZE = 1024;

		// statistics
		protected int insertCalls;
		protected int removeCalls;
		protected int containsCalls;
		protected int delta;
		protected static bool ok = true;

		// proxy factory for transactional objects
		protected XObjectFactory factory;

		// transactions
		protected XStart insertXStart;
		protected XStart removeXStart;
		protected XStart containsXStart;
		#endregion

		#region abstract methods
		/// <remarks>
		/// Inserts an element into the integer set, if it is not already present.
		/// </remarks>
		public abstract object Insert(params object[] v); 
  
		/// <remarks>
		/// Tests whether item is present.
		/// </remarks>
		/// <returns> whether the item was present</returns>
		public abstract object Contains(params object[] v); 
  
		/// <remarks>
		/// Remove item if present.
		/// </remarks>
		/// <returns> whether the item was removed</returns>
		public abstract object Remove(params object[] v);

		/// <summary>
		/// Enumerate over values in order
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerator GetEnumerator();

		
		public  object Done(params object[] v) 
		{
			Console.WriteLine("***DONE***");
			return 0;
		}
  
		#endregion

		#region Benchmark overrides
		/// <remarks>
		/// Checks that integer set is sorted and has correct size.
		/// </remarks>
		public override void Validate() 
		{
			int prior = Int32.MinValue;
			int size = 0;
			foreach (int i in this) 
			{
				if (i < prior) 
				{
					ReportError("Set not sorted");
					return;
				} 
				else if (i == prior) 
				{
					ReportError("Duplicate elements");
					return;
				}
				prior = i;
				size++;
			}
			int expected = INITIAL_SIZE + delta;
			if (size != expected)
				ReportError(String.Format("Set has wrong size, found {0}, expected {1}", size, expected));
			if (ok) 
			{
				Console.WriteLine("Data Structure OK");
			}
		}

		public override void Report() 
		{
			Console.WriteLine("Calls:");
			Console.WriteLine("\tInsert:\t\t{0}", insertCalls);
			Console.WriteLine("\tRemove:\t\t{0}", removeCalls);
			Console.WriteLine("\tContains:\t{0}", containsCalls);
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
			try 
			{
				while (true)	// will be interrupted by GracefulException
				{
					int element = random.Next();
					if (Math.Abs(element) % 100 < experiment) 
					{
						if (toggle) 
						{        // insert on even turns
							myInsertCalls++;
							value = element / 100;
							if ((bool)XAction.Run(insertXStart, value))
								myDelta++;
						} 
						else 
						{         // remove on odd turns
							myRemoveCalls++;
							if ((bool)XAction.Run(removeXStart, value))
								myDelta--;
						}
						toggle = !toggle;
					} 
					else 
					{
						XAction.Run(containsXStart, element / 100);
						myContainsCalls++;
					}
				}
			} 
			catch (GracefulException) 
			{
				// update statistics
				UpdateStatistic(ref insertCalls, myInsertCalls);
				UpdateStatistic(ref removeCalls, myRemoveCalls);
				UpdateStatistic(ref containsCalls, myContainsCalls);
				UpdateStatistic(ref delta, myDelta);
			}
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


		protected static void ReportError(String s) 
		{
			Console.WriteLine("ERROR: " + s);
			ok = false;
		}
		#endregion
	}
}
