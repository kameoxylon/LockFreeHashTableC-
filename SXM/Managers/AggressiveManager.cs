using System;
using System.Threading;
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Contention Manager that always aborts conflicting transactions.
	/// Often livelocks. Useful for performance baseline, not recommended for actual use.
	/// </summary>
	public class AggressiveManager : IContentionManager
	{

		#region Constructors
		public AggressiveManager()
		{
		}
		#endregion

		#region IContentionManager methods
		/// <summary>
		/// Immediately abort the writer.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		public void ResolveConflict(XState me, XState writer)
		{
			writer.Abort();
		}

		/// <summary>
		/// No priorities here
		/// </summary>
		public int Priority 
		{
			get {
				return 0;
			}
		}
	}
	#endregion
}
