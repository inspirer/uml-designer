using System;
using System.Collections;
using System.Drawing;
using UMLDes.Controls;

namespace UMLDes.GUI {

	public class StereoTypeHelper { 

		IHasStereotype obj;
		StaticView parent;
		string[] hashed_stereo_list;

		public StereoTypeHelper( IHasStereotype obj ) {
            this.obj = obj;
			this.parent = ((GuiObject)obj).parent;
		}

		private void EditedStereo( string ns ) {
			obj.Stereo = ns.Length > 0 ? ns : null;
		}

		private void set_stereo( object o, EventArgs ev ) {
			if( (o as FlatMenuItem).Index == 0 ) {
				Rectangle r = obj.EditRect;
				InPlaceTextEdit.Start( "Edit stereotype", obj.Stereo, parent.cview.point_to_screen(r.X, r.Y), Math.Max( r.Width, 70 ), r.Height, parent.cview, new StringEditedEvent( EditedStereo ), false );
				return;
			}

			if( (o as FlatMenuItem).Index >= 3 ) {
				obj.Stereo = hashed_stereo_list[(o as FlatMenuItem).Index-3];
			} else if( (o as FlatMenuItem).Index == 1 )
				obj.Stereo = null;
		}

		public FlatMenuItem GetStereoMenu() {

			FlatMenuItem curr;
			EventHandler evh;

			// Display Options
			evh = new EventHandler( set_stereo );
			curr = new FlatMenuItem( "Stereotype", null, 0, false );
			parent.AddItem( curr, "Other", ToolBarIcons.None, false, evh );
			parent.AddItem( curr, "Clear", ToolBarIcons.None, false, evh );
			parent.AddItem( curr, "-", ToolBarIcons.None, false, null );
			hashed_stereo_list = obj.StereoList;
			foreach( string s in hashed_stereo_list )
				if( s != null )
					parent.AddItem( curr, "\x00AB"+s+"\xBB", ToolBarIcons.None, false, evh );
				else
					parent.AddItem( curr, "-", ToolBarIcons.None, false, null );
			
            return curr;
		}

	}

}