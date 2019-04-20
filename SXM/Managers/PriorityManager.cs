using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// If prior transaction has later timestamp, abort it, else wait.
	/// </summary>
	public class PriorityManager : IContentionManager
	{
		#region fields

		// lock used for timetamps
		private static int clock = 0;
		#endregion

		#region Constructors
		public PriorityManager()
		{
		}
		#endregion

		#region IContentionManager methods
		/// <summary>
		/// Either give the writer a chance to finish it, abort it, or both.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
			if (writer.priority > me.priority) 
			{
				writer.BlockWhileActiveAndNotWaiting();	// wait for same or higher priority
			} 
			else 
			{
				writer.Abort();				// abort lower priority
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
