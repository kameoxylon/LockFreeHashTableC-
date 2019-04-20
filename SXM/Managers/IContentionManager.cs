using System;
using System.Collections;

namespace SXM 
{
	/// <summary>
	/// When transaction A is about to cause a conflict with transaction B,
	/// the Contention Manager controls whether A waits for B, aborts B, (or both).
	/// </summary>
	public interface IContentionManager 
	{
		/// <summary>
		/// Either give the writer a chance to finish it, abort it, or both.
		/// </summary>
		/// <param name="me">caller's XState object</param>
		/// <param name="writer">prior writer's XState object</param>
		void ResolveConflict(XState me, XState writer);

		int Priority 
		{
			get;
		}
	};
}