using Mijyuoon.MVVM.Impl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mijyuoon.MVVM {
    public abstract class BaseVM<T> : BaseModel, IEditableObject where T : class {
        private VMTypeMetadata metadata;
        private Dictionary<string, object> changes;

        public T Model { get; }
        public bool IsEditing { get; private set; }

        public BaseVM(T model) {
            this.Model = model ?? throw new ArgumentNullException(nameof(model));
            this.metadata = VMTypeMetadata.Get(typeof(T), GetType());

            if(model is INotifyPropertyChanged notify) {
                notify.PropertyChanged += ModelPropertyChanged;
            }
        }

        private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(IsEditing) return;

            OnPropertyChanged(e.PropertyName);
        }

        #region GetProperty

        private object GetPropertyImpl(VMTypeMetadata.PropertyInfo info, bool editing) {
            if(editing && changes.TryGetValue(info.Name, out var value))
                return value;
            
            return info.Getter(Model);
        }
 
        protected object GetProperty([CallerMemberName] string property = null) {
            if(!metadata.ModelProps.TryGetValue(property, out var pInfo))
                throw new ArgumentException($"Property '{property}' does not exist");

            if(pInfo.Getter is null)
                throw new ArgumentException($"Property '{property}' is not readable");

            return GetPropertyImpl(pInfo, IsEditing);
        }

        #endregion

        #region SetProperty

        private bool SetPropertyImpl(VMTypeMetadata.PropertyInfo info, object value, bool editing) {
            if(editing) {
                if(!changes.TryGetValue(info.Name, out var oldValue))
                    oldValue = info.Getter(Model);
                if(object.Equals(oldValue, value))
                    return false;

                changes[info.Name] = value;
            } else {
                var oldValue = info.Getter(Model);
                if(object.Equals(oldValue, value))
                    return false;

                info.Setter(Model, value);
            }

            OnPropertyChanged(info.Name);
            return true;
        }

        protected void SetProperty(object value, [CallerMemberName] string property = null) {
            if(!metadata.ModelProps.TryGetValue(property, out var pInfo))
                throw new ArgumentException($"Property '{property}' does not exist");

            if(pInfo.Getter is null || pInfo.Setter is null)
                throw new ArgumentException($"Property '{property}' is not read-writable");

            SetPropertyImpl(pInfo, value, IsEditing);
        }

        #endregion

        #region IEditableObject

        public void BeginEdit() {
            if(IsEditing)
                throw new InvalidOperationException("Editing is already started");

            if(changes is null)
                changes = new Dictionary<string, object>();

            IsEditing = true;
        }

        public void EndEdit() {
            if(!IsEditing)
                throw new InvalidOperationException("Editing has not been started");

            foreach(var kvp in changes) {
                var prop = metadata.ModelProps[kvp.Key];
                SetPropertyImpl(prop, kvp.Value, false);
            }

            changes.Clear();
            IsEditing = false;
        }

        public void CancelEdit() {
            if(!IsEditing)
                throw new InvalidOperationException("Editing has not been started");

            changes.Clear();
            IsEditing = false;
        }

        #endregion
    }
}
