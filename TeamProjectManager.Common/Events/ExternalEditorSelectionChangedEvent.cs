﻿using Microsoft.Practices.Prism.Events;

namespace TeamProjectManager.Common.Events
{
    /// <summary>
    /// Defines the event that is raised when the selected External Editor changed.
    /// </summary>
    public class ExternalEditorSelectionChangedEvent : CompositePresentationEvent<ExternalEditorSelectionChangedEventArgs>
    {
    }
}