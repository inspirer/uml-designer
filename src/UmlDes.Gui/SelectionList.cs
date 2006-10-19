using System;
using System.Collections;

namespace CDS.GUI {

	/// <summary>
	/// Array of GuiObjects, fixes selected property of GuiObject
	/// </summary>
	public class SelectionList : IEnumerable {

		public IEnumerator GetEnumerator() {
			return objs.GetEnumerator();
		}

		ArrayList objs = new ArrayList();

		public int Count {
			get {
				return objs.Count;
			}
		}

		public GuiObject this[ int i ] {
			get {
				if( i < 0 || i >= objs.Count )
					return null;
				return objs[i] as GuiObject;
			}
		}

		public int Add( GuiObject value ) {
			if( objs.Contains( value ) )
				return 0;
			if( value == null || value.selected )
				throw new ArgumentException( "wrong item for collection" );
			value.selected = true;
			value.SelectionChanged();
			value.Invalidate();
			return objs.Add( value );
		}

		public void Remove( GuiObject obj ) {
			if( !obj.selected )
				throw new ArgumentException( "item is not from the collection" );
			obj.selected = false;
			obj.SelectionChanged();
			obj.Invalidate();
			objs.Remove( obj );
		}

		public void Clear() {
			foreach( GuiObject o in objs ) {
				o.selected = false;
				o.SelectionChanged();
				o.Invalidate();
			}
			objs.Clear ();
		}
	}

}