using CameraUploaderApp.Model;
using CameraUploaderApp.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CameraUploaderApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> Buckets { get; set; } = new();
        public ObservableCollection<S3ObjectItem> Files { get; set; } = new();

        private RelayCommand _uploadCommand;

        public ICommand UploadCommand => _uploadCommand;

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
                    _uploadCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private readonly S3Bucket _s3Bucket;
        public MainWindowViewModel() 
        {
            _s3Bucket = new S3Bucket();
            // イベント
            _uploadCommand = new RelayCommand(Upload, CanUpload);
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
            var keys = await _s3Bucket.GetFilesInBucketAsync(SelectedBucket);

            Files.Clear();
            foreach (var key in keys)
            {
                Files.Add(new S3ObjectItem { Key = key, IsSelected = false});
            }
        }

        /// <summary>
        /// ファイルアップロード処理
        /// </summary>
        private async void Upload()
        {
            //string bucketName = "";
            string objectName = "";
            string filePath = "";

            // ダイアログのインスタンスを生成
            var dialog = new OpenFileDialog();

            // TODO:ファイルの種類を設定
            //dialog.Filter = "";

            // ダイアログを表示する
            if(dialog.ShowDialog() == true)
            {
                // オブジェクト名を設定（yyyy/MM/dd/ファイル名）
                objectName = string.Format("{0}{1}", DateTime.Today.ToString(@"yyyy\/MM\/dd/"), dialog.SafeFileName);
            }

            // ファイルアップロード処理
            var test = await _s3Bucket.UploadFileAsync(SelectedBucket, objectName, dialog.FileName);
        }

        private bool CanUpload()
        {
            // バケットが選択されている場合のみ実行可能
            return !string.IsNullOrEmpty(SelectedBucket);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
