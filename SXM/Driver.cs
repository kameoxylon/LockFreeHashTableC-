
using System;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Security.Permissions;
using System.Security;
using System.Runtime.CompilerServices;

namespace SXM
{
	/// <summary>
	/// Main program for SXM benchmarks.
	/// Reads arguments from sell, starts and stops threads, and reports statistics.
	/// </summary>
	public class Driver
	{
		const int DEFAULT_THREADS = 1;
		const int DEFAULT_MILLIS = 5000;
		const int DEFAULT_EXP = 1;
		const string DEFAULT_MANAGER = "SXM.BackoffManager";
    
		static void Main(string[] args) 
		{
			int numThreads = DEFAULT_THREADS;
			int runtime  = DEFAULT_MILLIS;
			int experiment = DEFAULT_EXP;
			Type managerType = null;
			string managerName = DEFAULT_MANAGER;

			string benchmarkName = "UNKNOWN";
			Type benchmarkType = null;

			AppDomain appDomain = AppDomain.CurrentDomain;
			Assembly[] assemblies = appDomain.GetAssemblies();
			AssemblyTitleAttribute a =
				(AssemblyTitleAttribute) Attribute.GetCustomAttribute(assemblies[1],
				typeof(AssemblyTitleAttribute));
			Console.WriteLine(a.Title);
			AssemblyCopyrightAttribute c = 
				(AssemblyCopyrightAttribute) Attribute.GetCustomAttribute(assemblies[1],
				typeof(AssemblyCopyrightAttribute));
			Console.WriteLine(c.Copyright);
			Console.WriteLine();
        
			// Parse and check the args
			int argc = 0;
				try 
				{
					while (argc < args.Length) 
					{
						String option = args[argc++];
						if (option.Equals("-m")) 
						{
							managerName= args[argc];
							managerType = Type.GetType(managerName);
						}
						else if (option.Equals("-b")) 
						{
							benchmarkName= args[argc];
							benchmarkType = Type.GetType(benchmarkName);
						}
						else if (option.Equals("-t"))
							numThreads = Int32.Parse(args[argc]);
						else if (option.Equals("-n"))
							runtime = Int32.Parse(args[argc]);
						else if (option.Equals("-e"))
							experiment = Int32.Parse(args[argc]);
						else 
						{
							UsageError("Unrecognized option: {0}", option);
							return;
						}
						argc++;
					}
					// Set up benchmark and contention manager class.
					if (managerType == null) 
					{
						UsageError("Could not find manager: {0}.", managerName); 
						return;
					}
					if (benchmarkType == null) 
					{
						UsageError("Could not find benchmark: {0}", benchmarkName); 
						return;
					}

					// install contention manager
					XAction.ManagerType = managerType;
					// construct benchmark								
					ConstructorInfo constructor = benchmarkType.GetConstructor(new Type[0]);
					if (constructor == null) 
					{
						throw new PanicException("No constructor for ", benchmarkType);
					}
					Benchmark benchmark = (Benchmark)constructor.Invoke(new object[0]);
        
					// Set up the benchmark
					Thread[] threads = new Thread[numThreads];
					Console.WriteLine("Benchmark:\t" + benchmarkType);
					Console.WriteLine("Manager:\t" + managerType);
					Console.WriteLine("Threads:\t" + numThreads);
					Console.WriteLine("Mix:\t\t" + experiment + "% modifier calls");
					Console.WriteLine("Milliseconds:\t" + runtime);

					benchmark.experiment = experiment;
					// set up threads
					ThreadStart threadStart = new ThreadStart(benchmark.Run);
					for (int i = 0; i < threads.Length; i++)
						threads[i] = new Thread(threadStart);

					// stopwatch on
					DateTime start = DateTime.Now;

					for (int i = 0; i < numThreads; i++)
						threads[i].Start();

					// let them run for a while
					System.Threading.Thread.Sleep(runtime);

					// ask them to stop
					XAction.stop = true;

					// wait for them to stop
					for (int i = 0; i < threads.Length; i++) 
						threads[i].Join();
        
					// stopwatch off
					DateTime stop = DateTime.Now;
                
					// Validate results of this benchmark
					benchmark.Validate();
					TimeSpan duration = stop - start;
					Console.WriteLine("Elapsed time: " + duration + " seconds.");
					Console.WriteLine();
					XAction.Report();
					Console.WriteLine("----------------------------------------");
					benchmark.Report();
				}
				catch (FormatException) 
				{
					UserError("Expected a number: " + args[argc]);
				}
				catch (SecurityException e)  
				{
					UserError("Security exception: {0}", e.Message);
					}
				catch (System.Reflection.TargetInvocationException e)
				{ 
					UserError("{0}", e.GetBaseException());
				}
				catch (PanicException p) 
				{ 
					Console.WriteLine(p.Cause); 
				}
				catch (Exception e) 
				{ 
					Console.WriteLine("Uncaught exception: {0}", e); 
				}
   
		} 

		/// <summary>
		/// If user does something wrong, print error and die.
		/// </summary>
		/// <param name="format">format string for message</param>
		/// <param name="args">additional arguments</param>
		static private void UserError(string format, params object[] args) 
		{
			Console.WriteLine();
			Console.WriteLine("USER ERROR:");
			Console.WriteLine(format, args);
			Console.WriteLine();
		}	

		static public void UsageError(string format, params object[] args) 
		{
			Console.WriteLine();
			Console.WriteLine("USAGE ERROR:");
			Console.WriteLine("usage: SXM.Driver -m classname -b benchmark [-t #threads] [-n #millis] [-e experiment#]");
		}

	}
}
