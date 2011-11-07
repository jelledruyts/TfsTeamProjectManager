using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace TeamProjectManager.Shell.Modules.StatusPanel
{
    [Export]
    public partial class StatusPanelView : UserControl
    {
        public StatusPanelView()
        {
            InitializeComponent();
        }

        [Import]
        public StatusPanelViewModel ViewModel
        {
            get
            {
                return (StatusPanelViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void statusHistoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void OnPopupDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var popupRoot = GetVisualAncestor<Border>(thumb);
            var yAdjust = popupRoot.ActualHeight + e.VerticalChange;
            var xAdjust = popupRoot.ActualWidth + e.HorizontalChange;
            if ((xAdjust >= 0) && (yAdjust >= 0))
            {
                popupRoot.Width = xAdjust;
                popupRoot.Height = yAdjust;
            }
        }

        /// <summary>
        /// Gets the visual ancestor of a visual.
        /// </summary>
        /// <typeparam name="T">The type of ancestor to retrieve.</typeparam>
        /// <param name="target">The target visual.</param>
        /// <returns>The visual ancestor of the target object of the specified type.</returns>
        private static T GetVisualAncestor<T>(DependencyObject target) where T : class
        {
            DependencyObject item;
            try
            {
                item = VisualTreeHelper.GetParent(target);
            }
            catch (InvalidOperationException)
            {
                // Thrown when the dependency object is not a visual.
                return null;
            }
            while (item != null)
            {
                T itemAsT = item as T;
                if (itemAsT != null) return itemAsT;
                item = VisualTreeHelper.GetParent(item);
            }

            return null;
        }
    }
}