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

	public class Foo: ICloneable
	{
		protected int field;
		int[] array;
		public Foo() 
		{
			this.field = 0;
			this.array = new int[8];
		}

		public Foo(int v) 
		{
			this.field = v;
		}
		public virtual int Field
		{
			get
			{
				return this.field;
			}
			set
			{
				this.field = value;
			}
		}
		#region ICloneable Members

		public object Clone()
		{
			// TODO:  Add Foo.Clone implementation
			return null;
		}

		#endregion

		public virtual int this[int i]
		{
			get
			{
				return array[i];
			}
			set
			{
				array[i] = value;
			}
		}
	}

		public class TList: Foo, IRecoverable
		{
			LocalDataStoreSlot readCache;  // thread-local read 
			LocalDataStoreSlot writeCache;  // thread-local write copy 
			STMObject stmObject;
			int TX_field;
    
			public TList()
			{
				readCache = Thread.AllocateDataSlot();
				writeCache = Thread.AllocateDataSlot();
				stmObject = new STMObject(new Foo(), readCache, writeCache);
			}
			public TList(int v)
			{
				readCache = Thread.AllocateDataSlot();
				writeCache = Thread.AllocateDataSlot();
				stmObject = new STMObject(new Foo(v), readCache, writeCache);
			}

			private Foo ReadObject() 
			{
				Foo list = (Foo)Thread.GetData(readCache);
				if (list != null) 
				{
					Xaction.myHits++;
				} 
				else 
				{
					list = (Foo)stmObject.OpenRead();
				}
				return list;
			}

			private Foo WriteObject() 
			{
				Foo list = (Foo)Thread.GetData(writeCache);
				if (list != null) 
				{
					Xaction.myHits++;
				} 
				else 
				{
					list = (Foo)stmObject.OpenWrite();
				}
				return list;
			}

			public override int Field
			{
				get
				{
					return ReadObject().Field;
				}
				set
				{
					WriteObject().Field = value;
				}
			}
			#region IRecoverable Members

			public void Backup()
			{
				TX_field = field;
			}
			public void Recover()
			{
				field = TX_field;
			}

			#endregion
		}
	}