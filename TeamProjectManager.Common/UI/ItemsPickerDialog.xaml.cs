using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TeamProjectManager.Common.UI
{
    /// <summary>
    /// A dialog that allows the user to choose items from a list.
    /// </summary>
    public partial class ItemsPickerDialog : Window
    {
        #region Properties

        /// <summary>
        /// Gets or sets the available items.
        /// </summary>
        public IEnumerable AvailableItems
        {
            get { return this.itemsListBox.ItemsSource; }
            set { this.itemsListBox.ItemsSource = value; }
        }

        /// <summary>
        /// Gets the selected items.
        /// </summary>
        public IList SelectedItems
        {
            get { return this.itemsListBox.SelectedItems; }
        }

        /// <summary>
        /// Gets the first item in the current selection or returns null if the selection is empty.
        /// </summary>
        public object SelectedItem
        {
            get { return this.itemsListBox.SelectedItem; }
        }

        /// <summary>
        /// Gets or sets a path to a value on the source object to serve as the visual representation of each object in the list.
        /// </summary>
        public string ItemDisplayMemberPath
        {
            get { return this.itemsListBox.DisplayMemberPath; }
            set { this.itemsListBox.DisplayMemberPath = value; }
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        public SelectionMode SelectionMode
        {
            get { return this.itemsListBox.SelectionMode; }
            set { this.itemsListBox.SelectionMode = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="ItemsPickerDialog"/> instance.
        /// </summary>
        public ItemsPickerDialog()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void itemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.okButton.IsEnabled = this.itemsListBox.SelectedItems != null && this.itemsListBox.SelectedItems.Count > 0;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
