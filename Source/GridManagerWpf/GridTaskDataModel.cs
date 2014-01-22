#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using GridManagerWpf.Annotations;
using GridSharedLibs;

#endregion

namespace GridManagerWpf
{
    public class GridTaskDataModel : INotifyPropertyChanged
    {
        private double _completion;
        private GridTaskState _state;

        public GridTaskDataModel()
        {
            TaskInstances = new ObservableCollection<GridSubTaskDataModel>();
        }

        public string Id { get; set; }

        public ObservableCollection<GridSubTaskDataModel> TaskInstances { get; private set; }

        public string Name { get; set; }
        public string DllLocation { get; set; }
        public string AssemblyName { get; set; }

        public TaskType Type { get; set; }

        public string TaskRepositoryName { get; set; }

        public string PlatformTarget { get; set; }
        public string ExecutionPlatform { get; set; }

        public InstanceCreatorType CreatorType { get; set; }

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