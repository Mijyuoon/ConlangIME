using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Mijyuoon.MVVM {
    public class RelayCommand : ICommand {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute) {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute) : this(execute, null) { }

        #region ICommand

        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => execute?.Invoke(parameter);

        #endregion
    }
}
