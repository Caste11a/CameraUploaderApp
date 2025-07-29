using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;

namespace CameraUploaderApp.Model
{
    public class S3ObjectItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                IsSelectedChanged?.Invoke();
            }
        }
        public string Key { get; set; }
        public string? LastModified {  get; set; }
        public long? ObjectSize {  get; set; }


        public static event Action IsSelectedChanged;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
