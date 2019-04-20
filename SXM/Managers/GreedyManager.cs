using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Tries to keep a maximal independent set running.
	/// If prior transaction is
	///		waiting or lower priority, then abort it.
	///		otherwise, wait for it to commit, abort, or wait
	/// </summary>
	public class GreedyManager : IContentionManager
	{
		#region fields

		// counter used as clock
		private static int clock = 0;
		#endregion

		#region Constructors
		public GreedyManager()
		{
		}
		#endregion

		#region IContentionManager methods
		/// <summary>
		/// If prior transaction is
		///		active and higher priority, then wait.
		/// 	active and lower priority, then abort it.
		///		waiting and higher priority, then adopt its priority and abort it.
		/// 	waiting and lower priority, then abort it.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
			if (writer.waiting || writer.priority < me.priority)
			{
				writer.Abort();
			} else
			{
				me.Waiting();					// announce that I'm waiting
				writer.BlockWhileActiveAndNotWaiting();	// wait for prior transaction to become not ACTIVE
				me.NotWaiting();				// announce that I'm no longer waiting
			}
		}

		/// <summary>
		/// less confusing if higher number is higher priority
		/// </summary>
		public int Priority 
		{
			get 
			{
				return Int32.MaxValue - Interlocked.Increment(ref clock);
			}
		}
		#endregion
	}
}
