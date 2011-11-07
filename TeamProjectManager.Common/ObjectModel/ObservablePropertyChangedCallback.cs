
namespace TeamProjectManager.Common.ObjectModel
{
    /// <summary>
    /// Represents the callback that is invoked when the effective property value of a dependency property changes.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="sender">The <see cref="ObservableObject"/> on which the property has changed value.</param>
    /// <param name="e">Event data that is issued by any event that tracks changes to the effective value of this property.</param>
    public delegate void ObservablePropertyChangedCallback<TProperty>(ObservableObject sender, ObservablePropertyChangedEventArgs<TProperty> e);
}