using System;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Transaction start: delegate with arbitrary args
	/// </summary>
	public delegate object XStart(params object[] args);

	/// <summary>
	/// Summary description for XAction.
	/// </summary>
	public abstract class XAction
	{
		public static object Run(XStart start, params object[] args) 
		{
			object result = null;
			depth++;
			// less confusing if higher number is higher priority
			long priority = Manager.Priority;
			while (!XAction.stop) 
			{
				xState = new XState(priority);
				try 
				{
					result = start(args);	// user's delegate
					depth--;
				} 
				catch (AbortedException e)	// aborted by synch conflict, retry
				{
					if (depth > 1) 
					{
						depth--;
						throw e;
					}
				}
				catch (RetryException e)		// user wants to retry
				{
					if (depth > 1) 
					{
						depth--;
						throw e;
					}
					// block until another transctions writes something I've read (aborting me)
					xState.BlockWhileActive();
				}
				depth--;
				if (depth > 0) 
				{
					return result;
				} 
				else 
				{
					if (xState.Commit()) 
					{
						XAction.myCommits++;
						return result;
					} 
					else 
					{
						XAction.myAborts++;
					}
				}
			}
			UpdateStatistic(ref commits, myCommits);
			UpdateStatistic(ref aborts, myAborts);
			throw new GracefulException();
		}

		// current transaction
		[System.ThreadStatic] protected static XState xState = XState.COMMITTED;

		// per-thread statistics
		[System.ThreadStatic] public static int myCommits = 0;
		[System.ThreadStatic] public static int myAborts = 0;

		// nesting
		[System.ThreadStatic] public static int depth = 0;

		// global statistics
		public static int commits = 0;
		public static int aborts = 0;

		// Contention manager type		
		static Type managerType;

		// per-thread contention manager
		[System.ThreadStatic] static IContentionManager manager;

		// tell threads when to stop
		public static bool stop;

		// each transaction takes a timestamp when it starts
		public static long clock = 0;

		/// <summary>
		/// Call this method to retry a transaction.
		/// </summary>
		public static void Retry() 
		{
			throw new RetryException();
		}

		public static XState XState
		{
			get
			{
				if (xState == null)
					xState = XState.COMMITTED;
				return xState;
			}
		}

		/// <summary>
		/// Contention manager type (XAction to all transactions)
		/// </summary>
		public static Type ManagerType
		{
			set
			{
				managerType = value;
			}
		} 
		
		/// <summary>
		/// Contention manager instance (one per thread)
		/// </summary>
		public static IContentionManager Manager
		{
			get
			{
				if (manager == null) 
				{
					ConstructorInfo constructor = managerType.GetConstructor(new Type[0]);
					manager = (IContentionManager)constructor.Invoke(new object[0]);
					if (manager == null) 
					{
						throw new PanicException("Cannot find contention manager: {0}",
							managerType.Name);
					}
				}
				return manager;
			}
		}

		/* This will be supported in future versions
		public static XStart OrElse(XStart first, XStart second)
		{
			XAction.OrElseXStart orElse = new OrElseXStart(first, second);
			return new XStart(orElse.Run);
		}
		*/

		/// <summary>
		/// Print report.
		/// </summary>
		public static void Report() 
		{
			Console.WriteLine("Transactions:");
			long total = commits + aborts;
			Console.WriteLine("\tCommitted:\t{0:N0}", commits);
			if (total > 0) 
			{
				float commitRatio = ((float) commits) / ((float) total);
				Console.WriteLine("\tCommit ratio:\t{0:P}\t\t{1} / {2}",
					commitRatio, commits, total);
			} 
			else 
			{
				Console.WriteLine("No transactions executed!");
			}
		}
		/// <summary>
		///  Atomically update statistic at end of run.
		/// </summary>
		/// <param name="stat">ref to statistic</param>
		/// <param name="value">add this value</param>
		protected static void UpdateStatistic(ref int stat, int value) 
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
		/// <summary>
		/// Combine two alternative delegatss. Not yet supported
		/// </summary>
		class OrElseXStart
		{
			private XStart first;
			private XStart second;

			public OrElseXStart(XStart first, XStart second) 
			{
				this.first = first;
				this.second = second;
			}

			public object Run(params object[] args) 
			{		
				// try first alternative
				try 
				{
					return first(args);
				} 
				catch (RetryException)	// on retry, try second alternative
				{
					return second(args);
				}	
			}
		}

	}

}
