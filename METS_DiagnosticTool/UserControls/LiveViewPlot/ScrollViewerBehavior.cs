using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot
{
    public class ScrollViewerBehavior
    {
        public static bool GetAutoScrollToTop(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToTopProperty);
        }

        public static void SetAutoScrollToTop(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToTopProperty, value);
        }

        public static double GetScrollToVerticalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(ScrollToVerticalOffsetProperty);
        }

        public static void SetScrollToVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(ScrollToVerticalOffsetProperty, value);
        }

        public static readonly DependencyProperty AutoScrollToTopProperty =
            DependencyProperty.RegisterAttached("AutoScrollToTop", typeof(bool), typeof(ScrollViewerBehavior), new PropertyMetadata(false, (o, e) =>
            {
                ScrollViewer scrollViewer = o as ScrollViewer;
                if (scrollViewer == null)
                    return;
                
                if ((bool)e.NewValue)
                {
                    scrollViewer.ScrollToTop();
                    SetAutoScrollToTop(o, false);
                }
            }));

        public static readonly DependencyProperty ScrollToVerticalOffsetProperty =
            DependencyProperty.RegisterAttached("ScrollToVerticalOffset", typeof(double), typeof(ScrollViewerBehavior), new PropertyMetadata((double)0, (o, e) =>
            {
                ScrollViewer scrollViewer = o as ScrollViewer;
                if (scrollViewer == null)
                    return;

                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
                SetScrollToVerticalOffset(o, (double)e.NewValue);
            }));
    }
}
