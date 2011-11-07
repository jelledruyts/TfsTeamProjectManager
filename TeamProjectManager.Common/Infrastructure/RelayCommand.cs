using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Implements the ICommand interface by relaying the implementation to existing methods.
    /// </summary>
    /// <remarks>
    /// Thanks to Josh Smith for the basis for this code: http://msdn.microsoft.com/en-us/magazine/dd419663.aspx.
    /// </remarks>
    public class RelayCommand : ICommand
    {
        #region Member Fields

        private readonly Action<object> execute;
        private readonly Predicate<object> canExecute;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this command, as it should be displayed on the UI.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the description of this command, as it should be displayed on the UI.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the input gestures associated with this command.
        /// </summary>
        public IList<InputGesture> InputGestures { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute predicate.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
            : this(execute, canExecute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute predicate.</param>
        /// <param name="name">The name of the command.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, string name)
            : this(execute, canExecute, name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute predicate.</param>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">The description of the command.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, string name, string description)
            : this(execute, canExecute, name, description, (IEnumerable<InputGesture>)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute predicate.</param>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">The description of the command.</param>
        /// <param name="inputGesture">The input gesture for triggering the command.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, string name, string description, InputGesture inputGesture)
            : this(execute, canExecute, name, description, new InputGesture[] { inputGesture })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute predicate.</param>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">The description of the command.</param>
        /// <param name="inputGestures">The input gestures for triggering the command.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, string name, string description, IEnumerable<InputGesture> inputGestures)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
            this.Name = name;
            this.Description = description;
            this.InputGestures = new List<InputGesture>();
            if (inputGestures != null)
            {
                foreach (var gesture in inputGestures)
                {
                    this.InputGestures.Add(gesture);
                }
            }
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns><see langword="true"/> if this command can be executed; otherwise, <see langword="false"/>.</returns>
        public bool CanExecute(object parameter)
        {
            return this.canExecute == null ? true : this.canExecute(parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                throw new InvalidOperationException("Cannot execute the requested command at this time.");
            }
            this.execute(parameter);
        }

        #endregion
    }
}