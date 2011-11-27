using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides attached properties to show help content when UI elements are focused.
    /// </summary>
    public static class HelpProvider
    {
        #region Fields

        private static IDictionary<UIElement, object> helpContent = new Dictionary<UIElement, object>();

        #endregion

        #region Attached Properties

        /// <summary>
        /// Gets the help content associated with the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to get the help content for.</param>
        /// <returns>The help content to get.</returns>
        public static object GetHelpContent(UIElement target)
        {
            return (string)target.GetValue(HelpContentProperty);
        }

        /// <summary>
        /// Sets the help content associated with the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to set the help content for.</param>
        /// <param name="content">The help content to set.</param>
        public static void SetHelpContent(UIElement target, object content)
        {
            target.SetValue(HelpContentProperty, content);
        }

        /// <summary>
        /// Identifies the Content attached property.
        /// </summary>
        public static readonly DependencyProperty HelpContentProperty = DependencyProperty.RegisterAttached("HelpContent", typeof(object), typeof(HelpProvider), new UIPropertyMetadata(null, OnHelpContentChanged));

        #endregion

        #region Event Handlers

        private static void OnHelpContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (UIElement)d;
            var helpContentValue = e.NewValue;

            // For content controls, register the actual content element.
            var contentControl = target as ContentControl;
            if (contentControl != null)
            {
                if (!contentControl.IsLoaded)
                {
                    // In case the content isn't loaded yet, defer the registration.
                    contentControl.Loaded += (sender, args) => { Register(contentControl.Content as UIElement, helpContentValue); };
                }
                target = contentControl.Content as UIElement;
            }

            Register(target, helpContentValue);
        }

        private static void Register(UIElement target, object helpContentValue)
        {
            if (target != null)
            {
                helpContent[target] = helpContentValue;

                // Subscribe to events to update the help content.
                target.GotFocus -= new RoutedEventHandler(OnTargetGotFocus);
                target.GotFocus += new RoutedEventHandler(OnTargetGotFocus);
                target.IsVisibleChanged -= new DependencyPropertyChangedEventHandler(OnTargetIsVisibleChanged);
                target.IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnTargetIsVisibleChanged);

                RefreshHelpContent();
            }
        }

        private static void OnTargetIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshHelpContent();
        }

        private static void OnTargetGotFocus(object sender, RoutedEventArgs e)
        {
            RefreshHelpContent();
        }

        private static void RefreshHelpContent()
        {
            // Determine the help content to show by looking at all registered UI elements.
            var elements = helpContent.Keys.Cast<UIElement>();

            // If there are elements that have focus, take the deepest one in the UI tree.
            var focused = elements.Where(e => e.IsVisible && e.IsFocused);
            var deepestFocused = focused.Where(f => !focused.Any(e => e != f && e.IsDescendantOf(f))).FirstOrDefault();
            if (deepestFocused != null)
            {
                OnHelpContentUpdateRequested(helpContent[deepestFocused]);
            }
            else
            {
                // If there are elements that are visible, take the highest one in the UI tree.
                var visible = elements.Where(e => e.IsVisible);
                var highestVisible = visible.Where(v => !visible.Any(e => e != v && e.IsAncestorOf(v))).FirstOrDefault();
                if (highestVisible != null)
                {
                    OnHelpContentUpdateRequested(helpContent[highestVisible]);
                }
                else
                {
                    // If none of the registered elements are visible, show empty help content.
                    OnHelpContentUpdateRequested(null);
                }
            }
        }

        #endregion

        #region HelpContentUpdateRequested Event

        private static void OnHelpContentUpdateRequested(object content)
        {
            if (HelpContentUpdateRequested != null)
            {
                HelpContentUpdateRequested(null, new HelpContentEventArgs(content));
            }
        }

        /// <summary>
        /// Raised when the help content should change.
        /// </summary>
        public static event EventHandler<HelpContentEventArgs> HelpContentUpdateRequested;

        #endregion
    }
}