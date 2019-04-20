using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Contention Manager based on exponential backoff.
	/// </summary>
	public class BackoffManager : IContentionManager
	{
		#region members
		const int MIN_LOG_BACKOFF = 0;
		const int MAX_LOG_BACKOFF = 1;

		int attempts = 0;

		Random random;

		#endregion

		#region Constructors
		public BackoffManager()
		{
			random = new Random();
		}
		#endregion

		#region IContentionManager methods	
		/// <summary>
		/// If exponential backoff fails, then abort the writer.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
			int sleep = random.Next(1 << attempts);
			Thread.Sleep(sleep);
			if (writer.State != XStates.ACTIVE) 
			{	
				attempts = 0;
			} 
			else 
			{
				writer.Abort();		// no more Mister Nice Guy.
				if (attempts < MAX_LOG_BACKOFF)
					attempts++;
			}
			return;
		}

		/// <summary>
		/// No priorities here
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
