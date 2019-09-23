using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mijyuoon.MVVM {
    public abstract class BaseModel : INotifyPropertyChanged {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        protected void OnPropertyChanged(params string[] properties) =>
            Array.ForEach(properties, OnPropertyChanged);

        #endregion

        #region SetProperty

        private bool CheckEquals<T>(T lhs, T rhs) =>
            EqualityComparer<T>.Default.Equals(lhs, rhs);

        protected bool SetProperty<T>(ref T stored, T value, [CallerMemberName] string property = null) {
            if(CheckEquals(stored, value))
                return false;

            stored = value;
            OnPropertyChanged(property);
            return true;
        }

        protected bool SetProperty<T>(T stored, T value, Action<T> action, [CallerMemberName] string property = null) {
            if(CheckEquals(stored, value))
                return false;

            action(value);
            OnPropertyChanged(property);
            return true;
        }

        #endregion
    }
}
