using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CameraUploaderApp.Model
{
    public class S3ObjectItem : INotifyPropertyChanged
    {
        public string Key { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
