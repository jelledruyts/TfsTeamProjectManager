using System;
using System.Collections.Generic;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event arguments for External Editor selection change events.
    /// </summary>
    public class ExternalEditorSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the External Editor that has been selected.
        /// </summary>
        public string SelectedExternalEditor { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalEditorSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="selectedExternalEditor">The External Editor that has been selected.</param>
        public ExternalEditorSelectionChangedEventArgs(string selectedExternalEditor)
        {
            this.SelectedExternalEditor = selectedExternalEditor;
        }
    }
}