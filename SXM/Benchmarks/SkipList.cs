using System;
using System.Collections;
using System.Resources;
using System.Reflection;
using System.Threading;

namespace SXM
{
	/// <summary>
	/// Represents a collection of key-and-value pairs.
	/// </summary>
	/// <remarks>
	/// The SkipList class based on the data structure created by William Pugh.
	/// </remarks> 
	public class SkipList : SXM.IntSetBenchmark
	{        

		#region Members
		// Maximum level any node in a skip list can have
		private const int MaxLevel = 32;

		// Probability factor used to determine the node level
		private const double Probability = 0.5;

		// The skip list header. It also serves as the NIL node.
		private Node header;

		// Random number generator for generating random node levels.
		private Random random = new Random();

		// Current maximum list level.
		private int listLevel;

		#endregion

		#region Constructors
		public SkipList()
		{
			int size = 0;
			this.factory = new XObjectFactory(typeof(Node));
			header = (Node)factory.Create(MaxLevel);
			listLevel = 1;
			for(int i = 0; i < MaxLevel; i++)
			{
				header[i] = header;
			}
			// initialize transaction objects
			insertXStart = new XStart(this.Insert);
			removeXStart = new XStart(this.Remove);
			containsXStart = new XStart(this.Contains);
			while (size < INITIAL_SIZE) 
			{
				if ((bool)Insert(random.Next()))
					size++;
			}
		}
		#endregion

		#region IntSetBenchmark overrides
		/// <summary>
		/// Adds an element with the provided key and value to the SkipList.
		/// </summary>
		/// <param name="key">
		/// The Object to use as the key of the element to add. 
		/// </param>
		/// <param name="value">
		/// The Object to use as the value of the element to add. 
		/// </param>
		public override object Insert(params object[] _key)
		{
			Node[] update = new Node[MaxLevel];
			int key = (int)_key[0];
			Node curr;
            
			// If key does not already exist in the skip list.
			if(!Search(key, out curr, update))
			{
				// Inseart key/value pair into the skip list.
				Insert(key, update);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Searches for the specified key.
		/// </summary>
		/// <param name="key">
		/// The key to search for.
		/// </param>
		/// <returns>
		/// Returns true if the specified key is in the SkipList.
		/// </returns>
		public override object Contains(params object[] _key)
		{
			Node[] dummy = new Node[MaxLevel];
			int key = (int)_key[0];
			Node curr;
			return Search(key, out curr, dummy);
		}

		/// <summary>
		/// Removes the element with the specified key from the SkipList.
		/// </summary>
		/// <param name="key">
		/// The key of the element to remove.
		/// </param>
		public override object Remove(params object[] _key)
		{
			Node[] update = new Node[MaxLevel];
			int key = (int)_key[0];
			Node curr;

			if(Search(key, out curr, update))
			{
				// Take the forward references that point to the node to be 
				// removed and reassign them to the nodes that come after it.
				for(int i = 0; i < listLevel && update[i][i] == curr; i++)
				{
					update[i][i] = curr[i];
				}
				return true;
			}
			return false;
		}

		public override IEnumerator GetEnumerator()
		{
			return new SkipListEnumerator(this);
		}
		#endregion

		#region Helper methods
		/// <summary>
		/// Inserts a keykey into the SkipList.
		/// </summary>
		/// <param name="key">
		/// The key to insert into the SkipList.
		/// </param>
		/// </param>
		/// <param name="update">
		/// An array of nodes holding references to places in the SkipList in 
		/// which the search for the place to insert the new key/value pair 
		/// dropped down one level.
		/// </param>
		private void Insert(object key, Node[] update)
		{
			// Get the level for the new node.
			int newLevel = GetNewLevel();

			// If the level for the new node is greater than the skip list 
			// level.
			if(newLevel > listLevel)
			{
				// Make sure our update references above the current skip list
				// level point to the header. 
				for(int i = listLevel; i < newLevel; i++)
				{
					update[i] = header;
				}

				// The current skip list level is now the new node level.
				Interlocked.CompareExchange(
					ref listLevel,
					listLevel + 1,
					listLevel);
			}

			// Create the new node.
			Node newNode = (Node)factory.Create(newLevel, key);

			// Insert the new node into the skip list.
			for(int i = 0; i < newLevel; i++)
			{
				// The new node forward references are initialized to point to
				// our update forward references which point to nodes further 
				// along in the skip list.
				newNode[i] = update[i][i];

				// Take our update forward references and point them toward  the new node. 
				update[i][i] = newNode;
			}
		}  

		/// <summary>
		/// Returns a level value for a new SkipList node.
		/// </summary>
		/// <returns>
		/// The level value for a new SkipList node.
		/// </returns>
		private int GetNewLevel()
		{
			int level = 1;

			// Determines the next node level.
			while(random.NextDouble() < Probability && level < MaxLevel && level <= listLevel)
			{
				level++;
			}

			return level;
		}

		/// <summary>
		/// Search for the specified key using the IComparable interface 
		/// implemented by each key.
		/// </summary>
		/// <param name="key">
		/// The key to search for.
		/// </param>
		/// <param name="curr">
		/// A SkipList node to hold the results of the search.
		/// </param>
		/// <param name="update">
		/// An array of nodes holding references to the places in the SkipList
		/// search in which the search dropped down one level.
		/// </param>
		/// <returns>
		/// Returns true if the specified key is in the SkipList.
		/// </returns>
		/// <remarks>
		/// Assumes each key inserted into the SkipList implements the 
		/// IComparable interface.
		/// 
		/// If the specified key is in the SkipList, the curr parameter will
		/// reference the node with the key. If the specified key is not in the
		/// SkipList, the curr paramater will either hold the node with the 
		/// first key value greater than the specified key or null indicating 
		/// that the search reached the end of the SkipList.
		/// </remarks>
		private bool Search(object _key, out Node curr, Node[] update)
		{        
            
			bool found = false; 
			int key = (int)_key;
			int comp;

			// Begin at the start of the skip list.
			curr = header;
            
			// Work our way down from the top of the skip list to the bottom.
			for(int i = listLevel - 1; i >= 0; i--)
			{ 
				// Get the comparable interface for the current key.
				comp = curr[i].Key;

				// While we haven't reached the end of the skip list and the 
				// current key is less than the search key.
				while(curr[i] != header && comp < key)
				{
					// Move forward in the skip list.
					curr = curr[i];
					// Get the comparable interface for the current key.
					comp = curr[i].Key;
				}

				// Keep track of each node where we move down a level. This 
				// will be used later to rearrange node references when 
				// inserting a new element.
				update[i] = curr;
			}

			// Move ahead in the skip list. If the new key doesn't already 
			// exist in the skip list, this should put us at either the end of
			// the skip list or at a node with a key greater than the search key.
			// If the new key already exists in the skip list, this should put 
			// us at a node with a key equal to the search key.
			curr = curr[0];

			// Get the comparable interface for the current key.
			comp = curr.Key;

			// If we haven't reached the end of the skip list and the 
			// current key is equal to the search key.
			if(curr != header && comp == key)
			{
				// Indicate that we've found the search key.
				found = true;
			}

			return found;
		} 

		#endregion

		#region Atomic classes

		/// <summary>
		/// Represents a node in the SkipList.
		/// </summary>
		[Atomic]
			public class Node: ICloneable
		{
			#region Fields

			// References to nodes further along in the skip list.
			private Node[] forward;

			// The key/value pair.
			private int key;

			#endregion

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
			public Node(int level, int key)
			{
				forward = new Node[level];
				this.key = key;
			}

			public virtual int Key
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

		#endregion

		#region SkipListEnumerator Class

		/// <summary>
		/// Enumerates the elements of a skip list.
		/// </summary>
		private class SkipListEnumerator : IEnumerator
		{  
			#region Fields

			// The skip list to enumerate.
			private SkipList list;

			// The current node.
			private Node current;

			// Keeps track of previous move result so that we can know 
			// whether or not we are at the end of the skip list.
			private bool moveResult = true;

			#endregion

			/// <summary>
			/// Initializes an instance of a SkipListEnumerator.
			/// </summary>
			/// <param name="list"></param>
			public SkipListEnumerator(SkipList list)
			{
				this.list = list;
				current = list.header;
			}

       
			#region IEnumerator Members

			/// <summary>
			/// Advances the enumerator to the next element of the skip list.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next 
			/// element; false if the enumerator has passed the end of the 
			/// skip list.
			/// </returns>
			public bool MoveNext()
			{
				// If the result of the previous move operation was true
				// we can still move forward in the skip list.
				if(moveResult)
				{
					// Move forward in the skip list.
					current = current[0];

					// If we are at the end of the skip list.
					if(current == list.header)
					{
						// Indicate that we've reached the end of the skip 
						// list.
						moveResult = false;
					}
				}

				return moveResult;
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before 
			/// the first element in the skip list.
			/// </summary>
			public void Reset()
			{
				current = list.header;
				moveResult = true;
			}

			/// <summary>
			/// Gets the current element in the skip list.
			/// </summary>
			public object Current
			{
				get
				{                    
					return current.Key;
				}
			}

			#endregion
		}   
		#endregion
	}
}
