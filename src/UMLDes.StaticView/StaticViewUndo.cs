using System;
using System.Collections;

namespace UMLDes.GUI {

	public class StaticViewUndo : Undo {

		Stack u = new Stack();   // of Operation
		Stack r = new Stack();
		IUndoNotification notify;

		public StaticViewUndo( IUndoNotification n ) {
			notify = n;
		}

		public override bool can_redo {
			get {
				return ( r.Count > 0 );
			}
		}

		public override bool can_undo {
			get {
				return ( u.Count > 0 );
			}
		}

		public void Push( Operation p, bool do_first ) {
			if( u.Count == 0 || p != u.Peek() )  // p == UndoTop, means that Top operation was modified, and no Push needed
				u.Push( p );
			r.Clear();
			if( do_first )
				p.Do();
			notify.RefreshUndoState();
		}

		public override void DoUndo() {
			if( u.Count > 0 ) {
				Operation q = u.Pop() as Operation;
				q.Undo();
				r.Push( q );
				notify.RefreshUndoState();
			}
		}

		public override void DoRedo() {
			if( r.Count > 0 ) {
				Operation q = r.Pop() as Operation;
				q.Do();
				u.Push( q );
				notify.RefreshUndoState();
			}
		}

		public override void KillStack() {
			u.Clear();
			r.Clear();
			notify.RefreshUndoState();
		}

		public object Top {
			get {
				return u.Count > 0 ? u.Peek() : null;
			}
		}
	}

	public class RemoveOperation : Operation {
		IRemoveable obj;

		public RemoveOperation( IRemoveable r ) {
			obj = r;
		}

		public override void Do() {
			obj.Destroy();
		}

		public override void Undo() {
			obj.Restore();
		}
	}

	public class CreateOperation : Operation {
		IRemoveable obj;

		public CreateOperation( IRemoveable r ) {
			obj = r;
		}

		public override void Do() {
			obj.Restore();
		}

		public override void Undo() {
			obj.Destroy();
		}
	}
}
