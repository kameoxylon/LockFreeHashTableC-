/*
 IMPORTANT:  READ BEFORE DOWNLOADING, COPYING, INSTALLING OR USING.
 By downloading, copying, installing or using the software you agree to this license.
 If you do not agree to this license, do not download, install, copy or use the software.

 Brown University License Agreement
 Copyright (c) 2003, Brown University 
 All rights reserved.

 Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: 
  •	Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
  •	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. 
  •	The name of Brown University may not be used to endorse or promote products derived from this software without specific prior written permission. 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Threading;

namespace SXM
{
	/// <summary>
	/// Keeps track of a transaction's state
	/// </summary>
	public class XStatus 
	{
    
		// Constants are used for running outside a transaction
		// and for initializing locators.
		public static XStatus COMMITTED = new XStatus(XState.COMMITTED);
		public static XStatus ABORTED   = new XStatus(XState.ABORTED);
		public static XStatus SNAP   = new XStatus(XState.SNAP);

		// Should really be an XState, but CompareExchange wants an integer argument.
		private int state;

			// Used by some contention managers
		public long priority;

		/// <summary>
		/// Create a new Transaction Info.
		/// </summary>
		/// <param name="priority">Higher number is higher priority</param>
		public XStatus(long priority)
		{
			this.state = (int)XState.ACTIVE;
			this.priority = priority;
		}

		/// <summary>
		/// Initialize XStatus constant.
		/// </summary>
		/// <param name="init">must be one of static state objects</param>
		private XStatus(XState init)
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
				(int)XState.COMMITTED,
				(int)XState.ACTIVE) == (int)XState.ACTIVE);
			// notify any waiting threads
			lock(this) 
			{
				Monitor.PulseAll(this);
			}
			return result;
		}           
		        
		/// <summary>
		/// Attempt ACTIVE to WAITING transition.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public void Wait() 
		{
			// atomic change ACTIVE -> WAITING
			bool result = (Interlocked.CompareExchange(
				ref state,
				(int)XState.WAITING,
				(int)XState.ACTIVE) == (int)XState.ACTIVE);
			// notify any waiting threads
			lock(this) 
			{
				Monitor.PulseAll(this);
			}
		}           		        
		/// <summary>
		/// Attempt WAITING to ACTIVE transition.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public void Active() 
		{
			bool result = (Interlocked.CompareExchange(
				ref state,
				(int)XState.ACTIVE,
				(int)XState.WAITING) == (int)XState.WAITING);
			// no need to notify waiting threads
		}           

		/// <summary>
		/// Attempt ACTIVE or WAITING to ABORTED transition.
		/// </summary>
		/// <returns>whether transition was sucessful</returns>
		public bool Abort() 
		{
			int current = state;
			bool result = false;
			// Try only if current state is ACTIVE or WAITING
			if (state == (int)XState.ACTIVE || state == (int)XState.WAITING)
			{
				result = (Interlocked.CompareExchange(
					ref state,
					(int)XState.ABORTED,
					current) == current);
				// notify any waiting threads
				lock(this) 
				{
					Monitor.PulseAll(this);
				}
			}
			return result;
		}

		/// <summary>
		/// Suspend caller until transaction leaves ACTIVE state.
		/// </summary>
		public void WaitWhileActive() 
		{
			lock (this) 
			{
				while (true) 
				{
					if (this.state == (int)XState.ACTIVE)
						Monitor.Wait(this);
					else
						return;
				}
			}
		}
		public override String ToString() 
		{
			return String.Format("XState[{0}, {1}]", priority, (XState)state);
		}

		public XState State
		{
			get
			{
				return (XState)state;
			}
		}
	}
}
