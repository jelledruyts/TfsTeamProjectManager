using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TeamProjectManager.Common.Infrastructure;

namespace TeamProjectManager.Modules.SourceControl
{
    public class BranchInfoToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var branch = value as BranchInfo;
            if (branch != null)
            {
                var tooltip = "Owner: {1}{0}Date Created: {2}{0}Child Branches: {3}{0}Child Branches (Recursive): {4}{0}Max. Tree Depth: {5}".FormatCurrent(Environment.NewLine, branch.Owner, branch.DateCreated, branch.Children.Length, branch.RecursiveChildCount, branch.MaxTreeDepth);
                if (!string.IsNullOrEmpty(branch.Description))
                {
                    tooltip += "{0}Description: {1}".FormatCurrent(Environment.NewLine, branch.Description);
                }
                return tooltip;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}