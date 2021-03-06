//
// ContextMenuExtensionsMac.cs
//
// Author:
//       Greg Munn <greg.munn@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
#if MAC
using AppKit;
#endif

namespace MonoDevelop.Components
{
	#if MAC
	static class ContextMenuExtensionsMac
	{
		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ContextMenu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			var nsMenu = FromMenu (menu);
			ShowContextMenu (parent, evt, nsMenu);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu)
		{
			var nsMenu = FromMenu (menu);
			ShowContextMenu (parent, x, y, nsMenu);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, NSMenu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			parent.GrabFocus ();

			Gtk.Application.Invoke (delegate {
				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var nsview = MonoDevelop.Components.Mac.GtkMacInterop.GetNSView (parent);
				var toplevel = parent.Toplevel as Gtk.Window;

				var nswindow = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (toplevel);

				int titleBarOffset;
				if (toplevel.TypeHint == Gdk.WindowTypeHint.Toolbar && toplevel.Type == Gtk.WindowType.Toplevel && toplevel.Decorated == false) {
					// Undecorated toplevel toolbars are used for auto-hide pad windows. Don't add a titlebar offset for them.
					titleBarOffset = 0;
				} else {
					titleBarOffset = MonoDevelop.Components.Mac.GtkMacInterop.GetTitleBarHeight () + 12;
				}

				var pt = new CoreGraphics.CGPoint (x, nswindow.Frame.Height - y - titleBarOffset);

				var tmp_event = NSEvent.MouseEvent (NSEventType.LeftMouseDown,
					pt,
					0, 0,
					nswindow.WindowNumber,
					null, 0, 0, 0);

				NSMenu.PopUpContextMenu (menu, tmp_event, nsview);
			});
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, NSMenu menu)
		{
			int x, y;

			parent.TranslateCoordinates (parent.Toplevel, (int)evt.X, (int)evt.Y, out x, out y);

			ShowContextMenu (parent, x, y, menu);
		}

		static NSMenuItem CreateMenuItem (ContextMenuItem item)
		{
			if (item.IsSeparator) {
				return NSMenuItem.SeparatorItem;
			}

			var menuItem = new NSMenuItem (item.Label, (s, e) => item.Click ());

			menuItem.Hidden = !item.Visible;
			menuItem.Enabled = item.Sensitive;
			menuItem.Image = item.Image.ToNSImage ();

			if (item is RadioButtonContextMenuItem) {
				var radioItem = (RadioButtonContextMenuItem)item;
				menuItem.State = radioItem.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
			} else if (item is CheckBoxContextMenuItem) {
				var checkItem = (CheckBoxContextMenuItem)item;
				menuItem.State = checkItem.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
			} 

			if (item.SubMenu != null && item.SubMenu.Items.Count > 0) {
				menuItem.Submenu = FromMenu (item.SubMenu);
			}

			return menuItem;
		}

		static NSMenu FromMenu (ContextMenu menu)
		{
			var result = new NSMenu () { AutoEnablesItems = false };

			foreach (var menuItem in menu.Items) {
				var item = CreateMenuItem (menuItem);
				result.AddItem (item);
			}

			return result;
		}

		static readonly Xwt.Toolkit macToolkit = Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac);

		public static NSImage ToNSImage (this Xwt.Drawing.Image image)
		{
			return (NSImage)macToolkit.GetNativeImage (image);
		}
	}
	#endif
}