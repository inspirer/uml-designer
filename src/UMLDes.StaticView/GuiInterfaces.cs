using System;
using System.Collections;
using System.Drawing;

namespace UMLDes.GUI {

	public interface IDrawable {
		void Invalidate();
		void Paint( Graphics g, Rectangle r, int offx, int offy );
		bool NeedRepaint( Rectangle page );	 
	}

	public interface INeedRefresh {
		void RefreshView( Graphics g );
	}

	/// (ux:int, uy:float) are universal coordinates for binding, are dependent on object
	public interface IUniversalCoords {
		void coord_getxy( int ux, float uy, out int x, out int y );
		bool coord_nearest( int x, int y, out int ux, out float uy );
		void translate_coords( ref int ux, ref float uy );
	}

	public interface IMoveable : IUniversalCoords, IDrawable {
		void Moving( int x, int y, ref int ux, ref float uy );
		void Moved();
		bool IsMoveable( int x, int y );
	}

	public interface IClickable {
		void LeftClick( bool dbl, int x, int y );
	}

	public interface IMoveRedirect : IUniversalCoords, IDrawable {
		IMoveable MoveRedirect( ref int ux, ref float uy );
	}

	public interface IMoveMultiple : IMoveable {
		bool CanMoveInGroup { get; }
		void ShiftShape( int dx, int dy );
	}

	public interface IDropMenu {
		void AddMenuItems( System.Windows.Forms.ContextMenu m, int x, int y );
	}

	public interface ISelectable : IUniversalCoords, IDrawable {
		bool TestSelected( Rectangle sel );
		bool HasPoint( int x, int y, out int ux, out float uy );
	}

	public interface IRemoveable : ISelectable {
		bool Destroy();
		void Restore();
	}

	public interface IRemoveableChild : IRemoveable {
		bool Unlink();
		void Relink();
	}

	public interface IAroundObject {
		Rectangle AroundRect { get; }
	}

	public interface IHasID {
		string ID { get; }
	}

	public interface IAcceptConnection : IUniversalCoords, IHasID {
		void add_connection_point( GuiConnectionPoint p );
		void remove_connection_point( GuiConnectionPoint p );
	}

	public interface IHasCenter { 
		Point Center { get; }
	}

	// GuiActive content depends on Source Code
	public interface IDynamicContent {
		void RefreshContent();
	}

	public interface IValidateConnection {
		bool validate_connection( IAcceptConnection obj, GuiConnection connection );
	}

	public interface IHyphenSupport {
		Geometry.Direction direction( int ux );
	}
}