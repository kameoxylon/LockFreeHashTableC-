using System;
using System.Threading;

namespace SXM
{
	/// <summary>
	/// Mutable object tht keeps track of a transaction's state
	/// </summary>
	public class XState 
	{    
		// Constants are used for running outside a transaction
		// and for initializing locators.
		public static XState COMMITTED = new XState(XStates.COMMITTED);
		public static XState ABORTED   = new XState(XStates.ABORTED);
		public static int TIMEOUT = 2000;	// 2 seconds?

		// Should really be an XStates, but CompareExchange wants an integer argument.
		private int state;

		// Used by some contention managers
		public long priority;

		// used by some synchronization managers
		public bool waiting;

		/// <summary>
		/// Create a new Transaction Info.
		/// </summary>
		/// <param name="priority">Higher number is higher priority</param>
		public XState(long priority)
		{
			this.state = (int)XStates.ACTIVE;
			this.priority = priority;
		}

		/// <summary>
		/// Initialize XState constant.
		/// </summary>
		/// <param name="init">must be one of static state objects</param>
		private XState(XStates init)
		{
			state = (int)init;
			// convenient to assign highest priority to constants
			priority = Int64.MaxValue;
		}
                           
		/// <summary>
		/// Attempt ACTIVE to COMMITTED transition.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public bool Commit() 
		{
			// atomic change ACTIVE -> COMMITTED
			bool result = (Interlocked.CompareExchange(
				ref state,
				(int)XStates.COMMITTED,
				(int)XStates.ACTIVE) == (int)XStates.ACTIVE);
			// notify any waiting threads
			lock(this) 
			{
				Monitor.PulseAll(this);
			}
			return result;
		}           
		        
		/// <summary>
		/// Start waiting.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public void Waiting() 
		{
			waiting = true;
			// notify any waiting threads
			lock(this) 
			{
				Monitor.PulseAll(this);
			}
		}
           		        
		/// <summary>
		/// Stop waiting.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public void NotWaiting() 
		{
			waiting = false;
			// no need to notify waiting threads
		}           

		/// <summary>
		/// Attempt ACTIVE to ABORTED transition.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public bool Abort() 
		{
			bool result = (Interlocked.CompareExchange(
				ref state,
				(int)XStates.ABORTED,
				(int)XStates.ACTIVE) == (int)XStates.ACTIVE);
			// notify any waiting threads
			lock(this) 
			{
				Monitor.PulseAll(this);
			}
			return result;
		}

		/// <summary>
		/// Suspend caller until transaction leaves ACTIVE state.
		/// </summary>
		public void BlockWhileActiveAndNotWaiting() 
		{
			lock (this) 
			{
				while (this.state == (int)XStates.ACTIVE && !this.waiting)
				{
					Monitor.Wait(this, TIMEOUT);
					if (XAction.stop) 
					{
						throw new GracefulException();
					}
				}
			}
		}
		/// <summary>
		/// Suspend caller until transaction leaves ACTIVE state.
		/// </summary>
		public void BlockWhileActive() 
		{
			lock (this) 
			{
				while (this.state == (int)XStates.ACTIVE)
				{
					Monitor.Wait(this, TIMEOUT);
					if (XAction.stop) 
					{
						throw new GracefulException();
					}
				}
			}
		}
		public override String ToString() 
		{
			return String.Format("XStates[{0}, {1}]", priority, (XStates)state);
		}

		public XStates State
		{
			get
			{
				return (XStates)state;
			}
		}
	}
}
