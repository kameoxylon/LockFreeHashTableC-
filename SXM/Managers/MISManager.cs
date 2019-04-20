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
using System.Collections;

namespace SXM
{
	/// <summary>
	/// Tries to keep a maximal independent set running.
	/// If prior transaction is
	///		waiting or lower priority, then abort it.
	///		otherwise, wait for it to commit, abort, or wait
	/// </summary>
	public class MISManager : IContentionManager
	{
		#region fields

		// current transaction
		#endregion

		#region Constructors
		public MISManager()
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
		#endregion
	}
}
