using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// If earlier transaction is ACTIVE, then wait. If it is WAITING, abort it.
	/// </summary>
	public class WaitManager : IContentionManager
	{
		#region fields

		// current transaction
		#endregion

		#region Constructors
		public WaitManager()
		{
		}
		#endregion

		#region IContentionManager methods		
		/// <summary>
		/// Either abort writer or give it a chance to finish.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
		{
			if (writer.waiting)
			{
				writer.Abort();
			} 
			else
			{
				me.Waiting();					// announce that I'm waiting
				writer.BlockWhileActiveAndNotWaiting();	// wait for prior transaction to become not ACTIVE
				me.NotWaiting();				// announce that I'm no longer waiting
			}
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
