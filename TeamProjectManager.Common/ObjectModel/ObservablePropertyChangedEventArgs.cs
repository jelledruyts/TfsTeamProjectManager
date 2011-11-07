using System;

namespace TeamProjectManager.Common.ObjectModel
{
    /// <summary>
    /// Provides data for observable property changed events.
    /// </summary>
    public abstract class ObservablePropertyChangedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the property that has changed.
        /// </summary>
        public ObservableProperty Property { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservablePropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        protected ObservablePropertyChangedEventArgs(ObservableProperty property)
        {
            this.Property = property;
        }

        #endregion
    }

    /// <summary>
    /// Provides data for observable property changed events.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    public class ObservablePropertyChangedEventArgs<TProperty> : ObservablePropertyChangedEventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the old property value.
        /// </summary>
        public TProperty OldValue { get; private set; }

        /// <summary>
        /// Gets or sets the new property value.
        /// </summary>
        public TProperty NewValue { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservablePropertyChangedEventArgs&lt;TProperty&gt;"/> class.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        public ObservablePropertyChangedEventArgs(ObservableProperty<TProperty> property, TProperty oldValue, TProperty newValue)
            : base(property)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        #endregion
    }
}