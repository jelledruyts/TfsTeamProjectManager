using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace TeamProjectManager.Modules.BuildDefinitions
{
    [Export]
    public partial class BuildDefinitionsView : UserControl
    {
        private readonly Timer _filterTimer;
        private string _filterText;
        private PropertyGroupDescription _groupDescriptor;

        public BuildDefinitionsView()
        {
            InitializeComponent();

            _filterText = string.Empty;
            _filterTimer = new Timer()
            {
                Interval = 350,
                AutoReset = false
            };
            _filterTimer.Elapsed += RefreshView;
        }

        [Import]
        public BuildDefinitionsViewModel ViewModel
        {
            get
            {
                return (BuildDefinitionsViewModel)this.DataContext;
            }
            set
            {
                this.DataContext = value;
            }
        }

        private void buildDefinitionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedBuildDefinitions = buildDefinitionsDataGrid.SelectedItems.Cast<BuildDefinitionInfo>().ToList();
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            BuildDefinitionInfo buildDefinitionInfo = e.Item as BuildDefinitionInfo;
            if (buildDefinitionInfo == null) return;

            if (string.IsNullOrEmpty(_filterText))
            {
                e.Accepted = true;
            }
            else if (Regex.IsMatch(buildDefinitionInfo.Name, _filterText, RegexOptions.IgnoreCase))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }


        private void RefreshView(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
                                  {
                                    _filterText = _filterTextBox.Text;
                                    CollectionViewSource.GetDefaultView(buildDefinitionsDataGrid.ItemsSource).Refresh();                                   
                                  }
            );
        }

        private void OnFilterTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            _filterTimer.Stop();
            _filterTimer.Start();
        }

        private void ContextMenu_OnGroupBy(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(buildDefinitionsDataGrid.ItemsSource);
            if (view == null || view.CanGroup != true) return;

            if (!view.GroupDescriptions.Cast<PropertyGroupDescription>().Any(desc => desc.PropertyName.Equals(_groupDescriptor.PropertyName)))
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(_groupDescriptor);
            }
            else
            {
                view.GroupDescriptions.Clear();                
            }
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            var header = sender as DataGridColumnHeader;

            if (header == null) return;

            var column = header.Column;
            _groupDescriptor = new PropertyGroupDescription(column.SortMemberPath);
        }
    }
}