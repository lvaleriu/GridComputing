using System.ComponentModel;
using GridManagerWpf.Annotations;
using GridSharedLibs;

namespace GridManagerWpf
{
    public class GridSubTaskDataModel : INotifyPropertyChanged
    {
        private double _completion;
        private GridTaskState _state;

        public string Id { get; set; }

        public double Completion
        {
            get { return _completion; }
            set
            {
                _completion = value;
                OnPropertyChanged("Completion");
            }
        }

        public GridTaskState State
        {
            get { return _state; }
            set
            {
                _state = value;
                OnPropertyChanged("State");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}