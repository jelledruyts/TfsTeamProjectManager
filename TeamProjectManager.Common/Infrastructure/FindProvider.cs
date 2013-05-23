using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides attached properties to enable Find commands on text boxes.
    /// </summary>
    public static class FindProvider
    {
        #region Fields

        private static readonly ICommand findCommand = new RoutedCommand("Find", typeof(FindProvider), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F, ModifierKeys.Control) }));
        private static readonly ICommand findNextCommand = new RoutedCommand("FindNext", typeof(FindProvider), new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F3) }));

        #endregion

        #region Attached Properties

        /// <summary>
        /// Gets a value that determines if find is enabled on the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to get the value for.</param>
        /// <returns>A value that determines if find is enabled.</returns>
        public static bool GetFindEnabled(FrameworkElement target)
        {
            if (target != null)
            {
                return (bool)target.GetValue(FindEnabledProperty);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets a value that determines if find is enabled on the <see cref="UIElement"/>
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to set the value for.</param>
        /// <param name="value">A value that determines if find is enabled.</param>
        public static void SetFindEnabled(FrameworkElement target, bool value)
        {
            if (target != null)
            {
                target.SetValue(FindEnabledProperty, value);
            }
        }

        /// <summary>
        /// Identifies the FindEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty FindEnabledProperty = DependencyProperty.RegisterAttached("FindEnabled", typeof(bool), typeof(FindProvider), new UIPropertyMetadata(false, OnFindEnabledChanged));

        private static void OnFindEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (FrameworkElement)d;
            var findEnabled = (bool)e.NewValue;
            if (!target.IsLoaded)
            {
                target.Loaded += (sender, args) => { Register(target, findEnabled); };
            }
            else
            {
                Register(target, findEnabled);
            }
        }

        /// <summary>
        /// Gets the text to find associated with the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to get the text to find for.</param>
        /// <returns>The text to find to get.</returns>
        public static string GetTextToFind(UIElement target)
        {
            return (string)target.GetValue(TextToFindProperty);
        }

        /// <summary>
        /// Sets the text to find associated with the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="UIElement"/> to set the text to find for.</param>
        /// <param name="value">The text to find to set.</param>
        public static void SetTextToFind(UIElement target, string value)
        {
            target.SetValue(TextToFindProperty, value);
        }

        /// <summary>
        /// Identifies the TextToFind attached property.
        /// </summary>
        public static readonly DependencyProperty TextToFindProperty = DependencyProperty.RegisterAttached("TextToFind", typeof(string), typeof(FindProvider), new UIPropertyMetadata(null));

        #endregion

        #region Helper Methods

        private static void Register(FrameworkElement target, bool findEnabled)
        {
            var existingFindBinding = target.CommandBindings.Cast<CommandBinding>().FirstOrDefault(c => c.Command == findCommand);
            if (findEnabled && existingFindBinding == null)
            {
                // Find is now enabled and it was not enabled previously, add command bindings and menu items.
                target.CommandBindings.Add(new CommandBinding(findCommand, ExecuteFind, CanExecuteFind));
                target.CommandBindings.Add(new CommandBinding(findNextCommand, ExecuteFindNext, CanExecuteFindNext));
                if (target.ContextMenu == null)
                {
                    target.ContextMenu = new ContextMenu();
                }
                target.ContextMenu.Items.Add(new MenuItem { Header = "Find", Command = findCommand });
                target.ContextMenu.Items.Add(new MenuItem { Header = "Find Next", Command = findNextCommand });
            }
            else if (!findEnabled && existingFindBinding != null)
            {
                // Find is now disabled and it was enabled previously, remove command bindings and menu items.
                target.CommandBindings.Remove(existingFindBinding);
                target.CommandBindings.Remove(target.CommandBindings.Cast<CommandBinding>().First(c => c.Command == findNextCommand));
                target.ContextMenu.Items.Remove(target.ContextMenu.Items.Cast<MenuItem>().First(m => m.Command == findCommand));
                target.ContextMenu.Items.Remove(target.ContextMenu.Items.Cast<MenuItem>().First(m => m.Command == findNextCommand));
                if (target.ContextMenu.Items.Count == 0)
                {
                    target.ContextMenu = null;
                }
            }
        }

        #endregion

        #region Find Commands

        private static void CanExecuteFind(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private static void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
        {
            var target = (UIElement)e.Source;
            var textToFind = GetTextToFind(target);
            textToFind = Microsoft.VisualBasic.Interaction.InputBox("Find what:", "Find", textToFind);
            PerformFind(target, textToFind);
            e.Handled = true;
        }

        private static void CanExecuteFindNext(object sender, CanExecuteRoutedEventArgs e)
        {
            var target = (UIElement)e.Source;
            var textToFind = GetTextToFind(target);
            e.CanExecute = !string.IsNullOrEmpty(textToFind);
        }

        private static void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
        {
            var target = (UIElement)e.Source;
            var textToFind = GetTextToFind(target);
            PerformFind(target, textToFind);
        }

        #endregion

        #region Find Implementation

        private static void PerformFind(UIElement target, string textToFind)
        {
            if (string.IsNullOrEmpty(textToFind))
            {
                return;
            }
            SetTextToFind(target, textToFind);
            var textBox = target as TextBox;
            if (textBox != null)
            {
                // Start searching at the caret position or after the current text selection (if there is any).
                var start = textBox.CaretIndex;
                if (textBox.SelectedText.Length > 0)
                {
                    start = start + textBox.SelectedText.Length;
                }

                var foundTextIndex = textBox.Text.Substring(start).IndexOf(textToFind, StringComparison.CurrentCultureIgnoreCase);

                // If nothing was found, search from the top.
                if (start > 0 && foundTextIndex < 0)
                {
                    start = 0;
                    foundTextIndex = textBox.Text.Substring(start).IndexOf(textToFind, StringComparison.CurrentCultureIgnoreCase);
                }

                if (foundTextIndex >= 0)
                {
                    textBox.Select(start + foundTextIndex, textToFind.Length);
                    textBox.ScrollToLine(textBox.GetLineIndexFromCharacterIndex(start + foundTextIndex));
                }
            }
        }

        #endregion
    }
}