using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Mijyuoon.MVVM {
    public class MappedCollection<Ti, To> : ReadOnlyObservableCollection<To> {
        private ObservableCollection<Ti> inputs;
        private ObservableCollection<To> outputs;
        private Func<Ti, To> outputFactory;

        public MappedCollection(ObservableCollection<Ti> inputs, Func<Ti, To> outputFactory) : 
        this(new ObservableCollection<To>(inputs.Select(outputFactory))) {
            this.inputs = inputs;
            this.outputFactory = outputFactory;

            inputs.CollectionChanged += InputsCollectionChanged;
        }

        private MappedCollection(ObservableCollection<To> outputs) : base(outputs) {
            this.outputs = outputs;
        }

        #region ModelsCollectionChanged

        private void InputsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch(e.Action) {
            case NotifyCollectionChangedAction.Add:
                Debug.Assert(e.NewItems.Count == 1, "#NewItems == 1");

                outputs.Insert(e.NewStartingIndex, outputFactory((Ti)e.NewItems[0]));
                break;

            case NotifyCollectionChangedAction.Move:
                Debug.Assert(e.NewItems.Count == 1, "#NewItems == 1");
                Debug.Assert(e.OldItems.Count == 1, "#OldItems == 1");

                outputs.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Remove:
                Debug.Assert(e.OldItems.Count == 1, "#OldItems == 1");

                outputs.RemoveAt(e.OldStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                Debug.Assert(e.NewItems.Count == 1, "#NewItems == 1");
                Debug.Assert(e.OldItems.Count == 1, "#OldItems == 1");
                Debug.Assert(e.OldStartingIndex == e.NewStartingIndex, "OldIndex == NewIndex");

                outputs[e.OldStartingIndex] = outputFactory((Ti)e.NewItems[0]);
                break;

            case NotifyCollectionChangedAction.Reset:
                outputs.Clear();
                break;
            }
        }

        #endregion
    }
}
