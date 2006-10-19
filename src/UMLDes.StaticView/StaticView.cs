//#define CONTAINING_RECT_DEBUG
using System;
using System.Collections;
using System.Drawing;
using System.Xml.Serialization;
using UMLDes.Controls;
using UMLDes.Model;

namespace UMLDes.GUI {

	/// <summary>
	/// Summary description for StaticView.
	/// </summary>
	[XmlInclude(typeof(GuiClass))]
	public class StaticView : View, IPostload, IUndoNotification {

		// list of GuiActive
        [ XmlElement("class", typeof(GuiClass)), 
		  XmlElement("enum", typeof(GuiEnum)),
		  XmlElement("memo", typeof(GuiMemo)),
		  XmlElement("package", typeof(GuiPackage)),
		  XmlElement("relation", typeof(GuiConnection))] 
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
			this.name = "StaticView1";
			mouseagent = new StaticViewMouseAgent(this);
			Undo = new StaticViewUndo(this);
		}

		StaticViewMouseAgent MouseAgent {
			get {
				return (StaticViewMouseAgent)mouseagent;
			}
		}

		#region Static View Toolbar

		UMLDes.Controls.FlatToolBarPanel drawingmode;
		UMLDes.Controls.FlatToolBarButton defbutton;

		public void SetDefaultDrawingMode() {
			MouseAgent.current_operation = MouseOperation.Select;
			drawingmode.MakeRadioDown( defbutton );
		}

		void ToolbarAction( int index ) {
			switch( (ToolBarIcons)index ) {
					// what to do
				case ToolBarIcons.arrow:
					MouseAgent.current_operation = MouseOperation.Select;
					break;
				case ToolBarIcons.conn_inher:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Inheritance;
					break;
				case ToolBarIcons.conn_assoc:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Association;
					break;
				case ToolBarIcons.conn_aggregation:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Aggregation;
					break;
				case ToolBarIcons.conn_composition:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Composition;
					break;
				case ToolBarIcons.conn_realiz:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Realization;
					break;
				case ToolBarIcons.conn_attachm:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Attachment;
					break;
				case ToolBarIcons.conn_dependence:
					MouseAgent.current_operation = MouseOperation.DrawConnection;
					MouseAgent.conn_type = UmlRelationType.Dependency;
					break;
				case ToolBarIcons.memo: 
					MouseAgent.current_operation = MouseOperation.DrawComment;
					break;
				case ToolBarIcons.package:
					MouseAgent.current_operation = MouseOperation.DrawPackage;
					break;
					// line type
				case ToolBarIcons.straight_conn: 
					MouseAgent.conn_style = GuiConnectionStyle.Line;
					break;
				case ToolBarIcons.segmented_conn: 
					MouseAgent.conn_style = GuiConnectionStyle.Segmented;
					break;
				case ToolBarIcons.quadric_conn: 
					MouseAgent.conn_style = GuiConnectionStyle.Quadric;
					break;
				case ToolBarIcons.curved_conn:
					MouseAgent.conn_style = GuiConnectionStyle.Besier;
					break;
			}
		}

		public override ArrayList LoadToolbars() {
			ArrayList l = new ArrayList();
			FlatToolBar toolbar = proj.tool_bar;

			UMLDes.Controls.MouseClickEvent m = new UMLDes.Controls.MouseClickEvent(ToolbarAction);
			UMLDes.Controls.FlatToolBarPanel p;

			// UML Elements drawing
			p = toolbar.AddPanel( 0, "UML" );
			l.Add( p );
			defbutton = p.AddButton( FlatButtonType.RadioDown, (int)ToolBarIcons.arrow, "Select", m );
			p.AddButton( FlatButtonType.Line, 0, null, null );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_inher, "Draw inhreitance", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_assoc, "Draw association", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_aggregation, "Draw aggregation", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_composition, "Draw composition", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_attachm, "Draw attachment", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_dependence, "Draw dependency/usage", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.conn_realiz, "Draw realization", m );
			p.AddButton( FlatButtonType.Line, 0, null, null );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.memo, "Draw memo", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.package, "Draw package", m );
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.constraint, "Draw constraint", m ).disabled = true;
			p.AddButton( FlatButtonType.Radio, (int)ToolBarIcons.actor, "Draw actor", m ).disabled = true;
			drawingmode = p;

			p = toolbar.AddPanel( 0, "Default line type" );
			l.Add( p );
			p.AddButton( MouseAgent.conn_style == GuiConnectionStyle.Line		? FlatButtonType.RadioDown : FlatButtonType.Radio, (int)ToolBarIcons.straight_conn, "Line", m );
			p.AddButton( MouseAgent.conn_style == GuiConnectionStyle.Segmented ? FlatButtonType.RadioDown : FlatButtonType.Radio, (int)ToolBarIcons.segmented_conn, "Segmented", m );
			p.AddButton( MouseAgent.conn_style == GuiConnectionStyle.Quadric	? FlatButtonType.RadioDown : FlatButtonType.Radio, (int)ToolBarIcons.quadric_conn, "Quadric", m );
			p.AddButton( MouseAgent.conn_style == GuiConnectionStyle.Besier	? FlatButtonType.RadioDown : FlatButtonType.Radio, (int)ToolBarIcons.curved_conn, "Bezier", m ).disabled = true;
			p.AddButton( FlatButtonType.Line, 0, null, null );
			p.AddButton( FlatButtonType.Simple, (int)ToolBarIcons.show_qual, "Show full qualified", m ).disabled = true;
			p.AddButton( FlatButtonType.Simple, (int)ToolBarIcons.oper_signature, "Operations signature", m ).disabled = true;

			return l;
		}

		#endregion 

		#region Item search

		public GuiObject FindItem( int x, int y, bool direct_search ) {
			int dx;
			float dy;

			return FindItem( x, y, out dx, out dy, direct_search );
		}

		// searchs for the children under the point (x,y)
		GuiObject FindItemInChildren( GuiObject obj, int x, int y, out int ux, out float uy ) {

			ux = 0; uy = 0f;
			
			foreach( GuiObject s in obj.children ) {
				if( s.children != null && !s.Hidden	) {
					GuiObject searched = FindItemInChildren( s, x, y, out ux, out uy );
					if( searched != null )
						return searched;
				}
				if( s is ISelectable && !s.Hidden && (s as ISelectable).HasPoint( x, y, out ux, out uy ) )
					return s;
			}

			return null;
		}

		public GuiObject FindItem( int x, int y, out int dx, out float dy, bool direct_search ) {

			GuiObject res = null;

			// search in selected GuiObjects
			if( direct_search ) {
				foreach( GuiObject b in SelectedObjects ) {
					if( b.children != null && !b.Hidden ) {
						res = FindItemInChildren( b, x, y, out dx, out dy );
						if( res != null )
							return res;
					}
					if( (b is ISelectable) && !b.Hidden && (b as ISelectable).HasPoint( x, y, out dx, out dy ) )
						return b;
				}

			} else {
				foreach( GuiObject i in SelectedObjects ) {
					GuiObject b = i;
					do {
						if( b.children != null ) {
							foreach( GuiObject sel in b.children )
								if( sel is ISelectable && !sel.Hidden && (sel as ISelectable).HasPoint(x, y, out dx, out dy))
									return sel;
						}
						b = (b is GuiBound) ? (b as GuiBound).root : null;
					} while( b != null );
				}
			}

			// search in other
			for( int i = active_objects.Count - 1; i >= 0 ; i-- ) {
				GuiObject p = (GuiObject)active_objects[i];
				if( p.children != null && !p.Hidden ) {
					res = FindItemInChildren( p, x, y, out dx, out dy );
					if( res != null )
						if( direct_search )
							return res;
						else
							return p;
				}
				if( p is ISelectable && !p.Hidden && (p as ISelectable).HasPoint(x, y, out dx, out dy ) )
					return p;
			}
			dx = 0; 
			dy = 0f;
			return null;
		}

		#endregion

		#region Paint/Refresh

		void PaintChildren( Graphics g, Rectangle r, int offx, int offy, IDrawable dr, Rectangle piece ) {
			GuiObject o = dr as GuiObject;
			foreach( GuiBound b in o.children ) 
				if( !b.Hidden ) {
					if( b.NeedRepaint( piece ) )
						b.Paint( g, r, offx, offy );
					if( b.children != null )
						PaintChildren( g, r, offx, offy, b, piece );
				}
		}

		public override void Paint( Graphics g, Rectangle r, int offx, int offy ) {
			Rectangle pagepiece = new Rectangle( offx, offy, r.Width, r.Height );
			foreach( IDrawable i in active_objects ) 
				if( !i.Hidden ) {
					if( i.NeedRepaint(pagepiece) )
						i.Paint( g, r, offx, offy );
					if( (i as GuiObject).children != null )
						PaintChildren( g, r, offx, offy, i, pagepiece );
				}

			mouseagent.DrawTemporary( g, r, offx, offy, pagepiece );

			#if CONTAINING_RECT_DEBUG

			foreach( IDrawable i in active_objects ) 
				g.DrawRectangle( Pens.Green, i.ContainingRect.X + r.X - offx, i.ContainingRect.Y + r.Y - offy, i.ContainingRect.Width, i.ContainingRect.Height );

			#endif
		}

		// forces object to recalculate its bounds
		public void RefreshObject( INeedRefresh obj ) {
			if( cview != null )
				using( Graphics g = Graphics.FromHwnd( cview.Handle ) )
					obj.RefreshView( g );
		}

		#endregion

		public void SelectInRectangle( Rectangle r ) {
			foreach( GuiObject i in active_objects )
				if( i is ISelectable && (i as ISelectable).TestSelected(r) ) {
					SelectedObjects.Add( i );
				}
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
				foreach( GuiBound b in o.children )
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
		public override void DoOperation(UMLDes.GUI.View.EditOperation op) {
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
				case EditOperation.SelectNone:
					SelectedObjects.Clear();
					break;
			}
		}

		// checks if an operation is available to enable it in the main menu
		public override bool IfEnabled(UMLDes.GUI.View.EditOperation op) {
			switch( op ) {
				case EditOperation.Delete:
					if( !cview.Focused )
						return false;
					foreach( GuiObject g in SelectedObjects )
						if( g is IRemoveable )
							return true;
					break;
				case EditOperation.SelectAll:
				case EditOperation.SelectNone:
					return true;
			}

			return false;
		}

		// 'the undo state is changing' notification
		public void RefreshUndoState() {
            proj.UpdateToolBar();
		}

		// 'AroundObject was moved' notification, all modified objects must be added to 'states' hash
		public void AroundObjectsMoved( ArrayList /* of IAroundObject */ r, Hashtable states ) {
            foreach( GuiObject o in active_objects )
				if( o is GuiConnection )
					if( (o as GuiConnection).CheckIntersection( r, states ) ) {
						o.Invalidate();
					}
		}

		public override bool IfContainsSmth(Rectangle r) {

			foreach( IDrawable i in active_objects )
				if( r.IntersectsWith( i.ContainingRect ) && !i.Hidden )
					return true;

			return false;
		}

		public override Rectangle GetContentRectangle() {
			Rectangle res = Rectangle.Empty;
			foreach( IDrawable i in active_objects )
				if( !i.Hidden )
					if( res.IsEmpty ) 
						res = i.ContainingRect;
					else
						res = Rectangle.Union( i.ContainingRect, res );
			return res;
		}

		#region Synchronize content on update

		public override void RefreshContent() {
            foreach( GuiObject o in active_objects )
				if( o is IDynamicContent )
					((IDynamicContent)o).RefreshContent();
			UpdateConnections();
			Undo.KillStack();
		}

		private void UpdateConnections() {

			// save current connections
			Hashtable ht = new Hashtable();
			foreach( GuiObject obj in active_objects ) 
				if( obj is GuiConnection ) {
					string id = ((GuiConnection)obj).relation_id;
					if( id != null )
						ht[id] = obj;
				}

			// add new relations
			for( int i = 0; i < active_objects.Count; i++ ) {
				GuiObject obj = (GuiObject)active_objects[i];
				if( obj is GuiClass ) {
					foreach( UmlRelation rel in RelationsHelper.GetRelations( ((GuiClass)obj).st, proj.model ) ) {
						if( ht.ContainsKey( rel.ID ) ) {
							((GuiConnection)ht[rel.ID]).AdjustRelation( rel );
							ht.Remove( rel.ID );
						} else {
							NewRelation( rel );
						}
					}
				}
			}

			// remove old
			foreach( GuiConnection old_conn in ht.Values ) {
				Destroy((IRemoveable)old_conn);
			}
		}

		public void NewRelation( UmlRelation rel ) {
			GuiClass c1 = FindClass(rel.src), c2 = FindClass(rel.dest);
			if( c1 != null && c2 != null ) {
				int ux1 = 1, ux2 = 3;
				float uy1 = .5f, uy2 = .5f;

				if( rel.type == UmlRelationType.Association ) {
					ux1 = 0;
					uy1 = c1.get_empty_point_on_edge( ux1 );
					ux2 = 2;
					uy2 = c2.get_empty_point_on_edge( ux2 );
				} else if( rel.type == UmlRelationType.Realization || rel.type == UmlRelationType.Inheritance ) {
					uy2 = c2.get_empty_point_on_edge( ux2 );
				}

				GuiConnection c = new GuiConnection( new GuiConnectionPoint( c1, ux1, uy1, 0 ), new GuiConnectionPoint( c2, ux2, uy2, 1 ), rel.type, this, rel.type == UmlRelationType.Attachment ? GuiConnectionStyle.Line : MouseAgent.conn_style );
				if( rel.type == UmlRelationType.Association )
					c.nav = GuiConnectionNavigation.Left;
				c.relation_id = rel.ID;
				c.first.UpdatePosition( true );
				c.second.UpdatePosition( true );
				c.DoCreationFixup();
				c.ConnectionCreated( this, rel.src_role, rel.dest_role, rel.name, rel.stereo );
				Undo.Push( new CreateOperation( c ), false );
			}
		}

		#endregion

		public GuiClass FindClass( UmlClass cl ) {
			foreach( GuiObject obj in active_objects ) {
                GuiClass gcl = obj as GuiClass;
				if( gcl != null && gcl.st == cl )
					return gcl;				
			}

			return null;
		}

		public void AddObject( GuiItem item, string name_base ) {
			item.parent = this;
			if( item is INeedRefresh )
				RefreshObject( (INeedRefresh)item );
			item.id = RegisterItemID( name_base, item );
			item.Invalidate();
			Undo.Push( new CreateOperation( (IRemoveable)item ), false );

			// add relations
			if( item is GuiClass ) {
				GuiClass cl = (GuiClass)item;
				for( int i = 0; i < active_objects.Count; i++ ) {
					GuiObject obj = (GuiObject)active_objects[i];
					if( obj is GuiClass )
						foreach( UmlRelation rel in RelationsHelper.GetRelations( ((GuiClass)obj).st, proj.model ) )
							if( rel.dest == cl.st || rel.src == cl.st )
								NewRelation( rel );
				}
			}
		}

		#region Popup Menu

		public void AddMenuItems( System.Windows.Forms.ContextMenu m ) {
			AddItem( m, "Show/Hide elements", ToolBarIcons.None, false, new EventHandler(showhide) );
		}

		public class WrappedElement : IVisible {

			StaticView view;
			GuiActive active;

			public WrappedElement( StaticView view, GuiActive active ) {
				this.view = view;
				this.active = active;
			}

			#region IVisible Members

			public bool Visible {
				get {
					return !active.RawHidden;
				}
				set {
					if( active.Hidden != !value )
						active.Hidden = !value;
				}
			}

			public string Name {
				get {
					return active.Name;
				}
			}

			public int ImageIndex { 
				get {
					if( active is GuiMemo )
						return (int)ToolBarIcons.memo;
					else if( active is GuiClass )
						return (int)ToolBarIcons.Class;
					else if( active is GuiPackage )
						return (int)ToolBarIcons.package;
					else if( active is GuiEnum )
						return (int)ToolBarIcons.Class;
					else if( active is GuiConnection ) {

						switch( ((GuiConnection)active).type ) {
							case UmlRelationType.Aggregation:
								return (int)ToolBarIcons.conn_aggregation;
							case UmlRelationType.Association:
								return (int)ToolBarIcons.conn_assoc;
							case UmlRelationType.Attachment:
								return (int)ToolBarIcons.conn_attachm;
							case UmlRelationType.Composition:
								return (int)ToolBarIcons.conn_composition;
							case UmlRelationType.Dependency:
								return (int)ToolBarIcons.conn_dependence;
							case UmlRelationType.Inheritance:
								return (int)ToolBarIcons.conn_inher;
							case UmlRelationType.Realization:
								return (int)ToolBarIcons.conn_realiz;
						}
					}
					return -1;
				}
			}

			#endregion
		}

		void showhide( object v, EventArgs ev ) {
			ArrayList l = new ArrayList();
			foreach( GuiActive mm in active_objects )
				l.Add( new WrappedElement( this, mm ) );

			ShowHideDialog.Process( cview.FindForm(), l, proj.icon_list );
		}

		#endregion

		#region Menu helper functions

		public void AddItem( UMLDes.Controls.FlatMenuItem fmi, string text, ToolBarIcons icon, bool Checked, EventHandler click_handler ) {
			UMLDes.Controls.FlatMenuItem curr = new UMLDes.Controls.FlatMenuItem( text, icon != ToolBarIcons.None ? proj.icon_list : null, (int)icon, Checked );
			if( click_handler != null )
				curr.Click += click_handler;
			else
				curr.Enabled = false;
			fmi.MenuItems.Add( curr );
		}

		public void AddItem( System.Windows.Forms.ContextMenu cm, string text, ToolBarIcons icon, bool Checked, EventHandler click_handler ) {
			UMLDes.Controls.FlatMenuItem curr = new UMLDes.Controls.FlatMenuItem( text, icon != ToolBarIcons.None ? proj.icon_list : null, (int)icon, Checked );
			if( click_handler != null )
				curr.Click += click_handler;
			else
				curr.Enabled = false;
			cm.MenuItems.Add( curr );
		}

		#endregion

		public void InvalidateAllAssociated( IStateObject obj ) {
			foreach( GuiObject o in obj.Associated ) {
				o.Invalidate();
				o.invalidate_children();
				if( o is IStateObject )
                    InvalidateAllAssociated( o as IStateObject );
			}
		}
	}
}
