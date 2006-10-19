using System;
using System.Collections;
using System.Drawing;
using CDS.CSharp;
using System.Xml.Serialization;

namespace CDS.GUI {

	/// <summary>
	/// Summary description for StaticView.
	/// </summary>
	[XmlInclude(typeof(GuiClass))]
	public class StaticView : View, IPostload, IUndoNotification {

		// list of GuiActive
        [XmlElement("class", typeof(GuiClass)), XmlElement("relation", typeof(GuiConnection))] 
		public ArrayList active_objects = new ArrayList();

		// list of GuiObject
		[XmlIgnore]	public SelectionList SelectedObjects = new SelectionList();  // list of GuiObject

		// Has of IHasID
		[XmlIgnore] public Hashtable gui_objects = new Hashtable();

		[XmlIgnore]	public int width, height;
		[XmlIgnore] public StaticViewUndo Undo;
		[XmlIgnore] public override Undo undo { get { return Undo; } }

		// list of IAroundObject
		[XmlIgnore] public ArrayList AroundObjects = new ArrayList();


		public StaticView() {
			this.name = "noname";
			mouseagent = new StaticViewMouseAgent(this);
			Undo = new StaticViewUndo(this);
		}

		public GuiObject FindItem( int x, int y, bool direct_search ) {
			int dx;
			float dy;

			return FindItem( x, y, out dx, out dy, direct_search );
		}

		// searchs for the children under the point (x,y)
		GuiObject FintItemInChildren( GuiObject obj, int x, int y, out int ux, out float uy ) {

			ux = 0; uy = 0f;
			
			foreach( GuiObject s in obj.children ) {
				if( s.children != null ) {
					GuiObject searched = FintItemInChildren( s, x, y, out ux, out uy );
					if( searched != null )
						return searched;
				}
				if( s is ISelectable )
					if( (s as ISelectable).HasPoint( x, y, out ux, out uy ) )
						return s;
			}

			return null;
		}

		public GuiObject FindItem( int x, int y, out int dx, out float dy, bool direct_search ) {

			GuiObject res = null;

			// search in selected GuiObjects
			if( direct_search ) {
				foreach( GuiObject b in SelectedObjects ) {
					if( b.children != null ) {
						res = FintItemInChildren( b, x, y, out dx, out dy );
						if( res != null )
							return res;
					}
					if( (b is ISelectable) && (b as ISelectable).HasPoint( x, y, out dx, out dy ) )
						return b;
				}

			} else {
				foreach( GuiObject i in SelectedObjects ) {
					GuiObject b = i;
					do {
						if( b.children != null ) {
							foreach( GuiObject sel in b.children )
								if( sel is ISelectable && (sel as ISelectable).HasPoint(x, y, out dx, out dy))
									return sel;
						}
						b = (b is GuiBinded) ? (b as GuiBinded).root : null;
					} while( b != null );
				}
			}

			// search in other
			for( int i = active_objects.Count - 1; i >= 0 ; i-- ) {
				GuiObject p = (GuiObject)active_objects[i];
				if( p.children != null ) {
					res = FintItemInChildren( p, x, y, out dx, out dy );
					if( res != null )
						if( direct_search )
							return res;
						else
							return p;
				}
				if( p is ISelectable && (p as ISelectable).HasPoint(x, y, out dx, out dy ) )
					return p;
			}
			dx = 0; 
			dy = 0f;
			return null;
		}

		public void SelectInRectangle( Rectangle r ) {
			foreach( GuiObject i in active_objects )
				if( i is ISelectable && (i as ISelectable).TestSelected(r) ) {
					SelectedObjects.Add( i );
				}
		}

		void PaintChildren( Graphics g, Rectangle r, int offx, int offy, IDrawable dr, Rectangle piece ) {
            GuiObject o = dr as GuiObject;
			foreach( GuiBinded b in o.children ) {
				if( b.NeedRepaint( piece ) )
					b.Paint( g, r, offx, offy );
				if( b.children != null )
					PaintChildren( g, r, offx, offy, b, piece );
			}
		}

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			Rectangle pagepiece = new Rectangle( offx, offy, r.Width, r.Height );
			foreach( IDrawable i in active_objects ) {
				if( i.NeedRepaint(pagepiece) )
					i.Paint( g, r, offx, offy );
				if( (i as GuiObject).children != null )
					PaintChildren( g, r, offx, offy, i, pagepiece );
			}

			mouseagent.DrawTemporary( g, r, offx, offy, pagepiece );
		}

		// fills gui_objects, AroundObjects (active_objects has been filled during XML load)
		private void PreLoadRegisterObjects( GuiObject o ) {

			if( o is IHasID ) {
				IHasID p = o as IHasID;
				if( p.ID == null )
					throw new ArgumentException( "no id" );
				if( gui_objects.ContainsKey( p.ID ) )
					throw new ArgumentException( "two identical ids" );
				gui_objects[p.ID] = o;
			}
			if( o is IAroundObject )
				AroundObjects.Add( o );
			if( o.children != null )
				foreach( GuiBinded b in o.children )
					PreLoadRegisterObjects( b );
		}

		// main PostLoad function in the diagram
		public override void PostLoad() {
			foreach( GuiObject p in active_objects ) {
				p.parent = this;
				PreLoadRegisterObjects( p );
			}

			foreach( GuiObject p in active_objects )
				p.PostLoad();
		}

		// forces object to recalculate its bounds
		public void RefreshObject( INeedRefresh obj ) {
			if( cview != null )
				using( Graphics g = Graphics.FromHwnd( cview.Handle ) )
					obj.RefreshView( g );
		}

		// registers object in: active_objects, gui_objects, AroundObjects
		public string RegisterItemID( string basename, object item ) {
			string b;
			int i = 0;

			do {
				i++;
				b = basename + "#" + i.ToString();
			} while( gui_objects.ContainsKey( b ) );

			gui_objects[b] = item;
			if( item is GuiActive ) {

				// classificators first (in the list)
				if( item is GuiItem )
					active_objects.Insert( 0, item );
				else 
					active_objects.Add( item );
			}
			if( item is IAroundObject )
				AroundObjects.Add( item );
			return b;
		}

		// Unlinks object from diagram
		public void UnregisterObject( string id, GuiObject o ) {
			gui_objects.Remove( id );
            active_objects.Remove( o );
			AroundObjects.Remove( o );
		}

		// tries to remove object from diagram with Undo
		public bool Destroy( IRemoveable o ) {
			bool res = o.Destroy();
			if( res )
				Undo.Push( new RemoveOperation( o ), false );
			return res;
		}

		// processes main menu operation
		public override void DoOperation(CDS.GUI.View.EditOperation op) {
			int i;

			switch( op ) {
				case EditOperation.Delete:
					i = 0;
					while( i < SelectedObjects.Count ) {
						GuiObject obj = SelectedObjects[i];
						IRemoveable o = obj as IRemoveable;
						if( o != null && Destroy(o) )
							SelectedObjects.Remove( obj );
						else
							i++;
					}
					break;
				case EditOperation.SelectAll:
					foreach( GuiObject o in active_objects )
						SelectedObjects.Add( o );
					break;
			}
		}

		// checks if an operation is available to enable it in the main menu
		public override bool IfEnabled(CDS.GUI.View.EditOperation op) {
			switch( op ) {
				case EditOperation.Delete:
					foreach( GuiObject g in SelectedObjects )
						if( g is IRemoveable )
							return true;
					break;
				case EditOperation.SelectAll:
					return true;
			}

			return false;
		}

		// 'the undo state is changing' notification
		public void RefreshUndoState() {
            proj.container.UpdateToolBar();
		}

		// 'AroundObject was moved' notification, all modified objects must be added to 'states' hash
		public void AroundObjectsMoved( ArrayList /* of IAroundObject */ r, Hashtable states ) {
            foreach( GuiObject o in active_objects )
				if( o is GuiConnection )
					if( (o as GuiConnection).CheckIntersection( r, states ) ) {
						o.Invalidate();
					}
		}
	}
}
