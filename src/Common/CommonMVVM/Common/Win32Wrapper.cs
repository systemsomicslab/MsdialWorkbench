using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Helper class to pass a WPF Window object to Win32 methods
    /// </summary>
    public sealed class Win32Wrapper : System.Windows.Forms.IWin32Window, System.Windows.Interop.IWin32Window
    {
        private readonly Func<IntPtr> handleGetter;

        public Win32Wrapper(Window window)
        {
            if (window == null)
                throw new ArgumentNullException("window");
            var interop = new WindowInteropHelper(window);
            handleGetter = () => interop.Handle;
        }

        public Win32Wrapper(System.Windows.Forms.Control control)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            handleGetter = () => control.Handle;
        }

        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get
            {
                return handleGetter();
            }
        }

        IntPtr System.Windows.Interop.IWin32Window.Handle
        {
            get
            {
                return handleGetter();
            }
        }
    }

    public static class WpfWin32WindowHelper
    {
        public static Win32Wrapper AsWin32(this Window window)
        {
            return new Win32Wrapper(window);
        }

        public static T GetDependencyObjectFromVisualTree<T>(DependencyObject startObject)
            // don't restrict to DependencyObject items, to allow retrieval of interfaces
            //where T : DependencyObject
            where T : class
        {
            //Walk the visual tree to get the parent(ItemsControl) 
            //of this control
            DependencyObject parent = startObject;
            while (parent != null)
            {
                T pt = parent as T;
                if (pt != null)
                    return pt;
                else
                    parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        public static Win32Wrapper OwnerAsWin32(this DependencyObject startObject)
        {
            var window = GetDependencyObjectFromVisualTree<Window>(startObject);
            if (window == null)
                return null;
            return new Win32Wrapper(window);
        }
    }
}
