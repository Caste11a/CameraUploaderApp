using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using CameraUploaderApp.Services;

namespace CameraUploaderApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> Buckets { get; set; } = new();
        public ObservableCollection<string> Files { get; set; } = new();

        private string _selectedBucket;
        public string SelectedBucket
        {
            get => _selectedBucket;
            set
            {
                if(_selectedBucket != value)
                {
                    _selectedBucket = value;
                    OnPropertyChanged();
                    LoadFilesAsync();
                }
            
            }
        }

        private readonly S3Bucket _s3Bucket;
        public MainWindowViewModel() 
        {
            _s3Bucket = new S3Bucket();
            Initialize();
        }

        private async void Initialize()
        {
            //S3_Basics.Main();
            // TODO:作成済のバケット情報取得
            List<string> buckets = await _s3Bucket.GetAllBucketsAsync();
            Buckets.Clear();
            foreach(var bucket in buckets)
            {
                Buckets.Add(bucket);
            }
        }

        private async void LoadFilesAsync()
        {
            if (string.IsNullOrEmpty(SelectedBucket)) return;
            List<string> files = await _s3Bucket.GetFilesInBucketAsync(SelectedBucket);
            Files.Clear();
            foreach (var file in files)
            {
                Files.Add(file);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
