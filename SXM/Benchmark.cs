using System;
using System.Threading;

namespace SXM
{
	/// <summary>
	///  A simple abstract class to set up uniform benchmarks
	///
	/// Maurice Herlihy
	/// August 2004
	/// </summary>
	public abstract class Benchmark
	{
		/// <remarks>
		/// Which experiment?
		/// </remarks>
		public int experiment;

		/// <remarks>
		/// Method run by each thread.
		/// </remarks>
		abstract public void Run();

		/// <remarks>
		/// Checks that everything makes sense after running the benchmark.
		/// </remarks>
		 abstract public void Validate();

		/// <remarks>
		/// prints out statistics
		/// </remarks>
		abstract public void Report();
	}
}
