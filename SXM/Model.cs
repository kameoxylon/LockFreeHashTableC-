using System;
using System.Threading;

namespace SXM
{

	public class Base: ICloneable
	{
		int field;
		int[] array;
		public Base() 
		{
			this.field = 0;
			this.array = new int[8];
		}
		public Base(int v) 
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

		public class Model: Base
		{
			SynchState synchState;
    
			public Model() : base()
			{
				synchState = new SynchState(this);
			}
			public Model(int v) : base(v)
			{
				synchState = new SynchState(this);
			}


			public override int Field
			{
				get
				{
					Base x = (Base)synchState.OpenRead();
					return x.Field;
				}
				set
				{
					Base x = (Base)synchState.OpenWrite();
					x.Field = value;
				}
			}
		}
	}