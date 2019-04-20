using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Contention Manager that never aborts conflicting transactions.
	/// Equivalent to locking. Can deadlock.
	/// Useful for performance baseline, not recommended for actual use.
	/// </summary>
	public class PassiveManager : IContentionManager
	{
		#region fields
		#endregion

		#region Constructors
		public PassiveManager()
		{
		}
		#endregion

		#region IContentionManager methods
		/// <summary>
		/// Wait for writer to finish.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
			while (writer.State == XStates.ACTIVE) 
			{
				writer.BlockWhileActiveAndNotWaiting();
			}
		}
		/// <summary>
		/// less confusing if higher number is higher priority
		/// </summary>
		public int Priority 
		{
			get 
			{
				return 0;
			}
		}
		#endregion
	}
}
