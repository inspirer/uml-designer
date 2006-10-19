using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using CDS.CSharp;

namespace CDS {

	public class MainWnd : CDS.Controls.FlatMenuForm {
		private System.ComponentModel.IContainer components;
		private CDS.Controls.FlatMenuItem menuItem4;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.TreeView project_files, project_classes, project_views;
		private System.Windows.Forms.ImageList treeImages;
		private CDS.Controls.FlatToolBar toolBar1;
		private System.Windows.Forms.ImageList toolbarImages;
		private CDS.Controls.FormsCollapserCtrl collapser;
		
		public Project p;
		private CDS.ViewCtrl ViewCtrl1;
		private CDS.Controls.FlatMenuItem menuItem26;
		private CDS.Controls.FlatMenuItem menuItem31;
		private CDS.Controls.FlatMenuItem menu_About;
		private CDS.Controls.FlatMenuItem menu_NewProject;
		private CDS.Controls.FlatMenuItem menu_OpenProject;
		private CDS.Controls.FlatMenuItem menu_SaveProject;
		private CDS.Controls.FlatMenuItem menu_SaveProjAs;
		private CDS.Controls.FlatMenuItem menu_Print;
		private CDS.Controls.FlatMenuItem menu_Exit;
		private CDS.Controls.FlatMenuItem menu_Undo;
		private CDS.Controls.FlatMenuItem menu_Cut;
		private CDS.Controls.FlatMenuItem menu_Copy;
		private CDS.Controls.FlatMenuItem menu_Paste;
		private CDS.Controls.FlatMenuItem menu_Delete;
		private CDS.Controls.FlatMenuItem menu_SelectAll;
		private CDS.Controls.FlatMenuItem menu_AddFiles;
		private CDS.Controls.FlatMenuItem menu_AddStaticView;
		private CDS.Controls.FlatMenuItem menu_Parse;
		private CDS.Controls.FlatMenuItem menumain_Help;
		private CDS.Controls.FlatMenuItem menumain_File;
		private CDS.Controls.FlatMenuItem menumain_Edit;
		private CDS.Controls.FlatMenuItem menumain_Project;
		private CDS.Controls.FlatMenuItem menu_Redo;
		private CDS.Controls.FlatMenuItem menu_GC_Collect;
		public ImageList list;
		
		public MainWnd() {
			InitializeComponent();
			PostInitialize();
			list = toolbarImages;
			
            TurnOnProject( Project.createNew() );
		}

		TreeView create_tree_view( string name ) {
			TreeView tv = new System.Windows.Forms.TreeView();

			tv.BackColor = System.Drawing.SystemColors.Window;
			tv.BorderStyle = System.Windows.Forms.BorderStyle.None;
			//this.tree.Dock = System.Windows.Forms.DockStyle.Left;
			tv.ImageList = this.treeImages;
			tv.Location = new System.Drawing.Point(0, 0);
			tv.Name = name;
			tv.Size = new System.Drawing.Size(208, 449);
			tv.TabIndex = 1;
			tv.LabelEdit = true;
			tv.BeforeLabelEdit +=new NodeLabelEditEventHandler(BeforeLabelEdit);
			tv.AfterLabelEdit +=new NodeLabelEditEventHandler(AfterLabelEdit);
			tv.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TreeMouseDown);
			tv.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TreeMouseUp);
			tv.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TreeMouseMove);
			tv.GiveFeedback += new System.Windows.Forms.GiveFeedbackEventHandler(this.tree_GiveFeedback);
			return tv;
		}

		CDS.Controls.FlatToolBarButton tool_undo, tool_redo, tool_cut, tool_copy, tool_paste;

		void PostInitialize() {
			this.project_files = create_tree_view( "project_files" );
			this.project_classes = create_tree_view( "project_classes" );
			this.project_views = create_tree_view( "project_views" );

			//
			//  collapser
			//
			this.collapser.parent_control = ViewCtrl1;
			this.collapser.AddControl( this.project_files, "Files", treeImages.Images[0] );
			this.collapser.AddControl( this.project_classes, "Classes", treeImages.Images[1] );
			this.collapser.AddControl( this.project_views, "Views", treeImages.Images[2] );

			//  ToolBar
			CDS.Controls.MouseClickEvent m = new CDS.Controls.MouseClickEvent(ProjectRelated);
			CDS.Controls.FlatToolBarPanel p = toolBar1.AddPanel( 0, "Standard" );
			p.AddButton( CDS.Controls.FlatButtonType.Simple, 0, "New project", m );
			p.AddButton( CDS.Controls.FlatButtonType.Simple, 1, "Open project", m );
			p.AddButton( CDS.Controls.FlatButtonType.Simple, 2, "Save", m );
			p.AddButton( CDS.Controls.FlatButtonType.Simple, 3, "Save all", m );
			p.AddButton( CDS.Controls.FlatButtonType.Line, 0, null, null );
			m = new CDS.Controls.MouseClickEvent(CutCopyPaste);
			tool_cut = p.AddButton( CDS.Controls.FlatButtonType.Simple, 4, "Cut", m );
			tool_copy = p.AddButton( CDS.Controls.FlatButtonType.Simple, 5, "Copy", m );
			tool_paste = p.AddButton( CDS.Controls.FlatButtonType.Simple, 6, "Paste", m );
			p.AddButton( CDS.Controls.FlatButtonType.Line, 0, null, null );
			tool_undo = p.AddButton( CDS.Controls.FlatButtonType.Simple, 15, "Undo", m );
			tool_redo = p.AddButton( CDS.Controls.FlatButtonType.Simple, 16, "Redo", m );

			tool_cut.disabled = tool_copy.disabled = tool_paste.disabled = true;

			p = toolBar1.AddPanel( 0, "Relations" );
			m = new CDS.Controls.MouseClickEvent(DrawingModeChanged);
			defbutton = p.AddButton( CDS.Controls.FlatButtonType.RadioDown, 7, "Select", m );
			p.AddButton( CDS.Controls.FlatButtonType.Radio, 8, "Draw connection", m );
			// TODO
			p.AddButton( CDS.Controls.FlatButtonType.Radio, 9, "Draw comment", m ).disabled = true;
			drawingmode = p;

			p = toolBar1.AddPanel( 0, "UML" );

			// Scale menu
			ComboBox cb = new ComboBox(); 
			cb.Size = new Size( 90, 20 );
			cb.DropDownStyle = ComboBoxStyle.DropDownList;
			cb.MaxDropDownItems = 15;

			for( int i = 0; i < scalevalue.Length; i += 2 )
				cb.Items.Add( (scalevalue[i] * 100 / scalevalue[i+1] ).ToString() + "%" );
			cb.SelectedIndex = 4;
			cb.SelectedIndexChanged += new EventHandler(ScaleChanged);
			scalecombo = cb;

			p.AddControl( cb );
		}

		#region ToolBar related

		void EnableButton( CDS.Controls.FlatToolBarButton b, bool en ) {
			if( !b.disabled != en ) {
				b.disabled = !b.disabled;
                b.parent.InvalidateButton( b );
			}
		}

		public void UpdateToolBar() {
			EnableButton( tool_undo, ViewCtrl1.Curr.undo.can_undo );
			EnableButton( tool_redo, ViewCtrl1.Curr.undo.can_redo );
		}

		void CutCopyPaste( int index ) {
			switch( index ) {
				case 15:   // Undo
					ViewCtrl1.Curr.undo.DoUndo();
					break;
				case 16:   // Redo
					ViewCtrl1.Curr.undo.DoRedo();
					break;
				default:
					MessageBox.Show( "CopyPaste" + index );
					break;
			}
		}

		void ProjectRelated( int index ) {
			switch( index ) {
				case 0:   // New
					break;
				case 1:   // Open
					LoadProject( null, null );
					break;
				case 2:   // Save
					SaveProject( null, null );
					break;
				case 3:   // Saveall
					break;
			}
		}

		CDS.Controls.FlatToolBarPanel drawingmode;
		CDS.Controls.FlatToolBarButton defbutton;

		void DrawingModeChanged( int index ) {
			index -= 7;
			ViewCtrl1.Curr.mouseagent.current_operation = index;
		}

		public void SetDefaultDrawingMode() {
			ViewCtrl1.Curr.mouseagent.current_operation = 0;
			drawingmode.MakeRadioDown( defbutton );
		}

		static int[] scalevalue = new int[] {
												3, 1,		// 300 %
												2, 1,		// 200 %
												3, 2,		// 150 %
												4, 3,		// 133 %
												1, 1,		// 100 %
												9, 10,		// 90 %
												3, 4,		// 75 %
												1, 2,		// 50 %
												1, 4,		// 25 %
		};

		ComboBox scalecombo;				

		protected void ScaleChanged( object v, EventArgs e ) {
			ViewCtrl1.SetupScale( scalevalue[scalecombo.SelectedIndex*2], scalevalue[scalecombo.SelectedIndex*2+1] );			
			SetDefaultDrawingMode();
		}

		#endregion
		
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if (components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}
		
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainWnd));
			this.menu_About = new CDS.Controls.FlatMenuItem();
			this.toolbarImages = new System.Windows.Forms.ImageList(this.components);
			this.menumain_Help = new CDS.Controls.FlatMenuItem();
			this.menu_GC_Collect = new CDS.Controls.FlatMenuItem();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menumain_File = new CDS.Controls.FlatMenuItem();
			this.menu_NewProject = new CDS.Controls.FlatMenuItem();
			this.menu_OpenProject = new CDS.Controls.FlatMenuItem();
			this.menu_SaveProject = new CDS.Controls.FlatMenuItem();
			this.menu_SaveProjAs = new CDS.Controls.FlatMenuItem();
			this.menuItem4 = new CDS.Controls.FlatMenuItem();
			this.menu_Print = new CDS.Controls.FlatMenuItem();
			this.menu_Exit = new CDS.Controls.FlatMenuItem();
			this.menumain_Edit = new CDS.Controls.FlatMenuItem();
			this.menu_Undo = new CDS.Controls.FlatMenuItem();
			this.menu_Redo = new CDS.Controls.FlatMenuItem();
			this.menuItem26 = new CDS.Controls.FlatMenuItem();
			this.menu_Cut = new CDS.Controls.FlatMenuItem();
			this.menu_Copy = new CDS.Controls.FlatMenuItem();
			this.menu_Paste = new CDS.Controls.FlatMenuItem();
			this.menu_Delete = new CDS.Controls.FlatMenuItem();
			this.menuItem31 = new CDS.Controls.FlatMenuItem();
			this.menu_SelectAll = new CDS.Controls.FlatMenuItem();
			this.menumain_Project = new CDS.Controls.FlatMenuItem();
			this.menu_AddFiles = new CDS.Controls.FlatMenuItem();
			this.menu_AddStaticView = new CDS.Controls.FlatMenuItem();
			this.menu_Parse = new CDS.Controls.FlatMenuItem();
			this.treeImages = new System.Windows.Forms.ImageList(this.components);
			this.toolBar1 = new CDS.Controls.FlatToolBar();
			this.collapser = new CDS.Controls.FormsCollapserCtrl();
			this.ViewCtrl1 = new CDS.ViewCtrl();
			this.SuspendLayout();
			// 
			// menu_About
			// 
			this.menu_About.Enabled = false;
			this.menu_About.ImageIndex = 14;
			this.menu_About.Images = this.toolbarImages;
			this.menu_About.Index = 0;
			this.menu_About.OwnerDraw = true;
			this.menu_About.Text = "&About";
			// 
			// toolbarImages
			// 
			this.toolbarImages.ImageSize = new System.Drawing.Size(16, 16);
			this.toolbarImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("toolbarImages.ImageStream")));
			this.toolbarImages.TransparentColor = System.Drawing.Color.Silver;
			// 
			// menumain_Help
			// 
			this.menumain_Help.ImageIndex = 0;
			this.menumain_Help.Images = null;
			this.menumain_Help.Index = 3;
			this.menumain_Help.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_About,
																						  this.menu_GC_Collect});
			this.menumain_Help.OwnerDraw = true;
			this.menumain_Help.Text = "&Help";
			this.menumain_Help.Popup += new System.EventHandler(this.Help_Popup);
			// 
			// menu_GC_Collect
			// 
			this.menu_GC_Collect.ImageIndex = 0;
			this.menu_GC_Collect.Images = null;
			this.menu_GC_Collect.Index = 1;
			this.menu_GC_Collect.OwnerDraw = true;
			this.menu_GC_Collect.Text = "GC.Collect";
			this.menu_GC_Collect.Click += new System.EventHandler(this.menu_GC_Collect_Click);
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menumain_File,
																					  this.menumain_Edit,
																					  this.menumain_Project,
																					  this.menumain_Help});
			// 
			// menumain_File
			// 
			this.menumain_File.ImageIndex = 0;
			this.menumain_File.Images = null;
			this.menumain_File.Index = 0;
			this.menumain_File.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_NewProject,
																						  this.menu_OpenProject,
																						  this.menu_SaveProject,
																						  this.menu_SaveProjAs,
																						  this.menuItem4,
																						  this.menu_Print,
																						  this.menu_Exit});
			this.menumain_File.OwnerDraw = true;
			this.menumain_File.Text = "&File";
			// 
			// menu_NewProject
			// 
			this.menu_NewProject.ImageIndex = 0;
			this.menu_NewProject.Images = this.toolbarImages;
			this.menu_NewProject.Index = 0;
			this.menu_NewProject.OwnerDraw = true;
			this.menu_NewProject.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menu_NewProject.Text = "&New project";
			this.menu_NewProject.Click += new System.EventHandler(this.menu_NewProject_Click);
			// 
			// menu_OpenProject
			// 
			this.menu_OpenProject.ImageIndex = 1;
			this.menu_OpenProject.Images = this.toolbarImages;
			this.menu_OpenProject.Index = 1;
			this.menu_OpenProject.OwnerDraw = true;
			this.menu_OpenProject.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.menu_OpenProject.Text = "&Open project";
			this.menu_OpenProject.Click += new System.EventHandler(this.LoadProject);
			// 
			// menu_SaveProject
			// 
			this.menu_SaveProject.ImageIndex = 2;
			this.menu_SaveProject.Images = this.toolbarImages;
			this.menu_SaveProject.Index = 2;
			this.menu_SaveProject.OwnerDraw = true;
			this.menu_SaveProject.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.menu_SaveProject.Text = "&Save project";
			this.menu_SaveProject.Click += new System.EventHandler(this.SaveProject);
			// 
			// menu_SaveProjAs
			// 
			this.menu_SaveProjAs.ImageIndex = 0;
			this.menu_SaveProjAs.Images = null;
			this.menu_SaveProjAs.Index = 3;
			this.menu_SaveProjAs.OwnerDraw = true;
			this.menu_SaveProjAs.Text = "Save &As ...";
			this.menu_SaveProjAs.Click += new System.EventHandler(this.SaveAsProject);
			// 
			// menuItem4
			// 
			this.menuItem4.ImageIndex = 0;
			this.menuItem4.Images = null;
			this.menuItem4.Index = 4;
			this.menuItem4.OwnerDraw = true;
			this.menuItem4.Text = "-";
			// 
			// menu_Print
			// 
			this.menu_Print.ImageIndex = 18;
			this.menu_Print.Images = this.toolbarImages;
			this.menu_Print.Index = 5;
			this.menu_Print.OwnerDraw = true;
			this.menu_Print.Text = "Print";
			this.menu_Print.Click += new System.EventHandler(this.menuItem13_Click);
			// 
			// menu_Exit
			// 
			this.menu_Exit.ImageIndex = 0;
			this.menu_Exit.Images = null;
			this.menu_Exit.Index = 6;
			this.menu_Exit.OwnerDraw = true;
			this.menu_Exit.Shortcut = System.Windows.Forms.Shortcut.CtrlX;
			this.menu_Exit.Text = "E&xit";
			this.menu_Exit.Click += new System.EventHandler(this.Exit);
			// 
			// menumain_Edit
			// 
			this.menumain_Edit.ImageIndex = 0;
			this.menumain_Edit.Images = null;
			this.menumain_Edit.Index = 1;
			this.menumain_Edit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						  this.menu_Undo,
																						  this.menu_Redo,
																						  this.menuItem26,
																						  this.menu_Cut,
																						  this.menu_Copy,
																						  this.menu_Paste,
																						  this.menu_Delete,
																						  this.menuItem31,
																						  this.menu_SelectAll});
			this.menumain_Edit.OwnerDraw = true;
			this.menumain_Edit.Text = "&Edit";
			this.menumain_Edit.Popup += new System.EventHandler(this.EditMenuPopup);
			// 
			// menu_Undo
			// 
			this.menu_Undo.ImageIndex = 15;
			this.menu_Undo.Images = this.toolbarImages;
			this.menu_Undo.Index = 0;
			this.menu_Undo.OwnerDraw = true;
			this.menu_Undo.Shortcut = System.Windows.Forms.Shortcut.CtrlZ;
			this.menu_Undo.Text = "&Undo";
			this.menu_Undo.Click += new System.EventHandler(this.menu_Undo_Click);
			// 
			// menu_Redo
			// 
			this.menu_Redo.ImageIndex = 16;
			this.menu_Redo.Images = this.toolbarImages;
			this.menu_Redo.Index = 1;
			this.menu_Redo.OwnerDraw = true;
			this.menu_Redo.Shortcut = System.Windows.Forms.Shortcut.CtrlY;
			this.menu_Redo.Text = "&Redo";
			this.menu_Redo.Click += new System.EventHandler(this.menu_Redo_Click);
			// 
			// menuItem26
			// 
			this.menuItem26.ImageIndex = 0;
			this.menuItem26.Images = null;
			this.menuItem26.Index = 2;
			this.menuItem26.OwnerDraw = true;
			this.menuItem26.Text = "-";
			// 
			// menu_Cut
			// 
			this.menu_Cut.ImageIndex = 4;
			this.menu_Cut.Images = this.toolbarImages;
			this.menu_Cut.Index = 3;
			this.menu_Cut.OwnerDraw = true;
			this.menu_Cut.Shortcut = System.Windows.Forms.Shortcut.CtrlX;
			this.menu_Cut.Text = "Cu&t";
			this.menu_Cut.Click += new System.EventHandler(this.menu_Cut_Click);
			// 
			// menu_Copy
			// 
			this.menu_Copy.ImageIndex = 5;
			this.menu_Copy.Images = this.toolbarImages;
			this.menu_Copy.Index = 4;
			this.menu_Copy.OwnerDraw = true;
			this.menu_Copy.Shortcut = System.Windows.Forms.Shortcut.CtrlC;
			this.menu_Copy.Text = "&Copy";
			this.menu_Copy.Click += new System.EventHandler(this.menu_Copy_Click);
			// 
			// menu_Paste
			// 
			this.menu_Paste.ImageIndex = 6;
			this.menu_Paste.Images = this.toolbarImages;
			this.menu_Paste.Index = 5;
			this.menu_Paste.OwnerDraw = true;
			this.menu_Paste.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
			this.menu_Paste.Text = "&Paste";
			this.menu_Paste.Click += new System.EventHandler(this.menu_Paste_Click);
			// 
			// menu_Delete
			// 
			this.menu_Delete.ImageIndex = 19;
			this.menu_Delete.Images = this.toolbarImages;
			this.menu_Delete.Index = 6;
			this.menu_Delete.OwnerDraw = true;
			this.menu_Delete.Shortcut = System.Windows.Forms.Shortcut.Del;
			this.menu_Delete.Text = "&Delete";
			this.menu_Delete.Click += new System.EventHandler(this.menuDeleteClick);
			// 
			// menuItem31
			// 
			this.menuItem31.ImageIndex = 0;
			this.menuItem31.Images = null;
			this.menuItem31.Index = 7;
			this.menuItem31.OwnerDraw = true;
			this.menuItem31.Text = "-";
			// 
			// menu_SelectAll
			// 
			this.menu_SelectAll.ImageIndex = 0;
			this.menu_SelectAll.Images = null;
			this.menu_SelectAll.Index = 8;
			this.menu_SelectAll.OwnerDraw = true;
			this.menu_SelectAll.Shortcut = System.Windows.Forms.Shortcut.CtrlA;
			this.menu_SelectAll.Text = "Select &All";
			this.menu_SelectAll.Click += new System.EventHandler(this.menu_SelectAll_Click);
			// 
			// menumain_Project
			// 
			this.menumain_Project.ImageIndex = 0;
			this.menumain_Project.Images = null;
			this.menumain_Project.Index = 2;
			this.menumain_Project.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																							 this.menu_AddFiles,
																							 this.menu_AddStaticView,
																							 this.menu_Parse});
			this.menumain_Project.OwnerDraw = true;
			this.menumain_Project.Text = "&Project";
			// 
			// menu_AddFiles
			// 
			this.menu_AddFiles.ImageIndex = 20;
			this.menu_AddFiles.Images = this.toolbarImages;
			this.menu_AddFiles.Index = 0;
			this.menu_AddFiles.OwnerDraw = true;
			this.menu_AddFiles.Shortcut = System.Windows.Forms.Shortcut.CtrlF;
			this.menu_AddFiles.Text = "&Add files";
			this.menu_AddFiles.Click += new System.EventHandler(this.AddFiles);
			// 
			// menu_AddStaticView
			// 
			this.menu_AddStaticView.ImageIndex = 13;
			this.menu_AddStaticView.Images = this.toolbarImages;
			this.menu_AddStaticView.Index = 1;
			this.menu_AddStaticView.OwnerDraw = true;
			this.menu_AddStaticView.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftN;
			this.menu_AddStaticView.Text = "&Add static view";
			this.menu_AddStaticView.Click += new System.EventHandler(this.menu_AddStaticView_Click);
			// 
			// menu_Parse
			// 
			this.menu_Parse.ImageIndex = 17;
			this.menu_Parse.Images = this.toolbarImages;
			this.menu_Parse.Index = 2;
			this.menu_Parse.OwnerDraw = true;
			this.menu_Parse.Shortcut = System.Windows.Forms.Shortcut.F5;
			this.menu_Parse.Text = "Parse files, refresh tree";
			this.menu_Parse.Click += new System.EventHandler(this.RefreshProject);
			// 
			// treeImages
			// 
			this.treeImages.ImageSize = new System.Drawing.Size(16, 16);
			this.treeImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("treeImages.ImageStream")));
			this.treeImages.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// toolBar1
			// 
			this.toolBar1.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(219)), ((System.Byte)(216)), ((System.Byte)(209)));
			this.toolBar1.Dock = System.Windows.Forms.DockStyle.Top;
			this.toolBar1.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(219)), ((System.Byte)(216)), ((System.Byte)(209)));
			this.toolBar1.images = this.toolbarImages;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(712, 24);
			this.toolBar1.TabIndex = 10;
			// 
			// collapser
			// 
			this.collapser.BackColor = System.Drawing.SystemColors.Control;
			this.collapser.Dock = System.Windows.Forms.DockStyle.Left;
			this.collapser.Location = new System.Drawing.Point(0, 24);
			this.collapser.Name = "collapser";
			this.collapser.Size = new System.Drawing.Size(22, 385);
			this.collapser.TabIndex = 11;
			// 
			// ViewCtrl1
			// 
			this.ViewCtrl1.AllowDrop = true;
			this.ViewCtrl1.Curr = null;
			this.ViewCtrl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ViewCtrl1.Location = new System.Drawing.Point(22, 24);
			this.ViewCtrl1.Name = "ViewCtrl1";
			this.ViewCtrl1.Size = new System.Drawing.Size(690, 385);
			this.ViewCtrl1.TabIndex = 12;
			// 
			// MainWnd
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(712, 409);
			this.Controls.Add(this.ViewCtrl1);
			this.Controls.Add(this.collapser);
			this.Controls.Add(this.toolBar1);
			this.Location = new System.Drawing.Point(0, 0);
			this.Menu = this.mainMenu1;
			this.Name = "MainWnd";
			this.Text = "C# UML Designer";
			this.ResumeLayout(false);

		}
		#endregion
		
		[STAThread]
		static void Main() {
			Application.Run(new MainWnd());
		}

		private bool SaveChanges() {
			if( p.modified ) {
				DialogResult r = MessageBox.Show( "Project has been modified. Do you want to save changes?", "Warning!", MessageBoxButtons.YesNoCancel );
				if( r == DialogResult.Cancel )
					return false;
				if( r == DialogResult.Yes )
					p.Save( false );
			}
			return true;
		}

		protected override void OnClosing(CancelEventArgs e) {
#if !DEBUG
			if( !SaveChanges() )
				e.Cancel = true;
#endif
			base.OnClosing (e);
		}

		#region Menu actions

		private void menu_NewProject_Click(object sender, System.EventArgs e) {
			if( !SaveChanges() )
				return;
			TurnOnProject( Project.createNew() );
		}
		
		private void Exit(object sender, System.EventArgs e) {
			this.Close();
		}

	
		private void SaveProject(object sender, System.EventArgs e) {
			p.Save( false );
		}

		private void SaveAsProject(object sender, System.EventArgs e) {
			p.Save( true );
		}
		
		private void LoadProject(object sender, System.EventArgs e) {
			Project q = Project.Load(this);
			if( q != null )
				TurnOnProject( q );
		}
		
		private void RefreshView() {

			project_files.Nodes.Clear();
			project_files.Nodes.Add( p.files );
			p.files.Expand();

			project_classes.Nodes.Clear();
			project_classes.Nodes.Add( p.classes );
			p.classes.Expand();

			project_views.Nodes.Clear();
			project_views.Nodes.Add( p.views );
			p.views.Expand();
		}

		public void RefreshTitle( ) {
			this.Text = "C# UML Designer: " + p.name + " [" + ViewCtrl1.Curr.name + "]";
		}

		private void TurnOnProject( Project p ) {

			if( p.diagrams.Count == 0 )
				return;

			if( this.p != null )
				this.p.container = null;

			this.p = p;
			p.container = this;
			RefreshView();
			SelectView( (GUI.View)p.diagrams[0], true );
			UpdateToolBar();
		}

		public void SelectView( GUI.View v, bool update ) {
			ViewCtrl1.Curr = v;
			if( update ) {
				RefreshTitle();
				ViewCtrl1.Invalidate();
			}
		}
		
		private void AddFiles(object sender, System.EventArgs e) {
			p.AddFile();
		}
		
		private void RefreshProject(object sender, System.EventArgs e) {
			p.Refresh();
		}

		private void menuItem13_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Print();
		}

		private void menu_AddStaticView_Click(object sender, System.EventArgs e) {
            CDS.GUI.View v = p.newStaticView();
			SelectView( v, true );
		}

		#endregion

		#region Edit menu operations

		private void EditMenuPopup(object sender, System.EventArgs e) {
			menu_Undo.Enabled = ViewCtrl1.Curr.undo.can_undo;
			menu_Redo.Enabled = ViewCtrl1.Curr.undo.can_redo;
			menu_Delete.Enabled = ViewCtrl1.Curr.IfEnabled( CDS.GUI.View.EditOperation.Delete );
			menu_Cut.Enabled = ViewCtrl1.Curr.IfEnabled( CDS.GUI.View.EditOperation.Cut );
			menu_Copy.Enabled = ViewCtrl1.Curr.IfEnabled( CDS.GUI.View.EditOperation.Copy );
			menu_Paste.Enabled = ViewCtrl1.Curr.IfEnabled( CDS.GUI.View.EditOperation.Paste );
			menu_SelectAll.Enabled = ViewCtrl1.Curr.IfEnabled( CDS.GUI.View.EditOperation.SelectAll );
		}

		private void menu_Undo_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.undo.DoUndo();
		}

		private void menu_Redo_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.undo.DoRedo();
		}

		private void menu_Copy_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( CDS.GUI.View.EditOperation.Copy );
		}

		private void menu_Paste_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( CDS.GUI.View.EditOperation.Paste );
		}

		private void menu_Cut_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( CDS.GUI.View.EditOperation.Cut );
		}

		private void menuDeleteClick(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( CDS.GUI.View.EditOperation.Delete );
		}

		private void menu_SelectAll_Click(object sender, System.EventArgs e) {
			ViewCtrl1.Curr.DoOperation( CDS.GUI.View.EditOperation.SelectAll );
		}

		#endregion

		#region Drag & Drop and other tree ops

		Rectangle dragbox = Rectangle.Empty;
		CS_Element dragobject;
		
		void TreeMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			dragbox = Rectangle.Empty;
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left ) {
				TreeNode node = (sender as TreeView).GetNodeAt( e.X, e.Y);
				if( node != null && node.Tag != null ) {

					if( node.Tag is CDS.GUI.View ) {
						if( e.Clicks == 2 ) {
							CDS.GUI.View v = node.Tag as CDS.GUI.View;
							SelectView( v, true );
						}

					} else if( !(node.Tag is Project) ) {
						Size dragSize = SystemInformation.DragSize;
						dragbox = new Rectangle(new Point(e.X - (dragSize.Width /2), e.Y - (dragSize.Height /2)), dragSize);				
						dragobject = (CS_Element)node.Tag;
					}
				}
			}
		}
		
		void TreeMouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left ) {
				if( dragbox != Rectangle.Empty && !dragbox.Contains( e.X,e.Y) ) {
					ViewCtrl1.DragObject = dragobject;
					DragDropEffects dropEffect = ((TreeView)sender).DoDragDrop( dragobject.name, DragDropEffects.Copy );
					///....
					dragbox = Rectangle.Empty;
				}
			}
		}
		
		void TreeMouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			if( (e.Button & MouseButtons.Left) == MouseButtons.Left )
				dragbox = Rectangle.Empty;
		}

		private void BeforeLabelEdit(object sender, NodeLabelEditEventArgs e) {
			e.CancelEdit = !( e.Node.Tag != null && (e.Node.Tag is CDS.GUI.View ) );
		}

		private void AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
			if( e.Node.Tag is CDS.GUI.View ) {
				CDS.GUI.View v = e.Node.Tag as CDS.GUI.View;
				v.name = e.Label;
			}
		}

		private void tree_GiveFeedback(object sender, System.Windows.Forms.GiveFeedbackEventArgs e) {

			e.UseDefaultCursors = false;
			if ((e.Effect & DragDropEffects.Copy) == DragDropEffects.Copy)
				Cursor.Current = Cursors.Hand;
			else 
				Cursor.Current = Cursors.No;
		}

		#endregion

		#region Help menu

		private void Help_Popup(object sender, System.EventArgs e) {
			menu_GC_Collect.Text = "GC.Collect (" + GC.GetTotalMemory(false)/1024 + " Kb alloc)";
		}

		private void menu_GC_Collect_Click(object sender, System.EventArgs e) {
			GC.Collect();
		}
		#endregion

	}
}
