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

/// Skip list benchmark
/// adapted from codeby: Leslie Sanford (08/27/2003)
using System;
using System.Collections;
using System.Resources;
using System.Reflection;

namespace SXM
{
	/// <summary>
	/// Represents a collection of key-and-value pairs.
	/// </summary>
	/// <remarks>
	/// The Tryout class based on the data structure created by William Pugh.
	/// </remarks> 
	public class Tryout : SXM.Benchmark
	{        
		private Node header;
		protected Xaction testXaction;
		protected XObjectFactory factory;

		public Tryout()
		{
			this.factory = new XObjectFactory(typeof(Node));
			header = (Node)factory.Create(8);
			testXaction = new Xaction(new Xaction.Delegate(Test));
		}

		public void Test() 
		{
			header.Key = 2004;
			Console.WriteLine("key is {0}, expected 2004", header.Key);
			header[0] = header;
			if (header[0] == header)
				Console.WriteLine("header OK");
		}
		/// <remarks>
		/// Method run by each thread.
		/// </remarks>
		public override void Run() 
		{
			testXaction.Run();
		}

		/// <remarks>
		/// Checks that everything makes sense after running the benchmark.
		/// </remarks>
		public override void Validate() {}

		/// <remarks>
		/// prints out statistics
		/// </remarks>
		public override void Report() {}
	}

	/// <summary>
	/// Represents a node in the Tryout.
	/// </summary>
	[Atomic]
	public class Node: ICloneable
	{
		// References to nodes further along in the skip list.
		private Node[] forward;

		// The key/value pair.
		private object key;

		/// <summary>
		/// No-arg constructor
		/// </summary>
		public Node()
		{
		}
		/// <summary>
		/// Initializes an instant of a Node with its node level.
		/// </summary>
		/// <param name="level">
		/// The node level.
		/// </param>
		public Node(int level)
		{
			forward = new Node[level];
		}

		/// <summary>
		/// Initializes an instant of a Node with its node level and 
		/// key/value pair.
		/// </summary>
		/// <param name="level">
		/// The node level.
		/// </param>
		/// <param name="key">
		/// The key for the node.
		/// </param>
		/// <param name="val">
		/// The value for the node.
		/// </param>
		public Node(int level, object key)
		{
			forward = new Node[level];
			this.key = key;
		}

		public virtual object Key
		{
			get
			{
				return this.key;
			}
			set
			{
				this.key = value;
			}
		}

		public object Clone() 
		{
			Node newNode = new Node();
			newNode.key = this.key;
			newNode.forward = (Node[])this.forward.Clone();
			return newNode;
		}

		public virtual Node this[int i]
		{
			get
			{
				return this.forward[i];
		}
			set
			{
				this.forward[i] = value;
			}
		}
	}

}