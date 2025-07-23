using CameraUploaderApp.Model;
using CameraUploaderApp.Services;
using CommonPlatform.Native;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Windows.Input;

namespace CameraUploaderApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> Buckets { get; set; } = new();
        public ObservableCollection<S3ObjectItem> Files { get; set; } = new();

        private RelayCommand _uploadCommand;
        private RelayCommand _downloadCommand;
        private RelayCommand _deleteCommand;

        public ICommand UploadCommand => _uploadCommand;
        public ICommand DownloadCommand => _downloadCommand;
        public ICommand DeleteCommand => _deleteCommand;

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
                    _downloadCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        // プログレス用バー
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private readonly S3Bucket _s3Bucket;
        public MainWindowViewModel() 
        {
            _s3Bucket = new S3Bucket();
            // イベント
            _uploadCommand = new RelayCommand(Upload, CanUpload);
            _downloadCommand = new RelayCommand(Download);
            _deleteCommand = new RelayCommand(DeleteObjects);

            int result_init = FujiCameraNative.CallXsdkInit();
            if (result_init == (int)FujiCameraNative.RESULT.XSDK_COMPLETE)
            {
                
                //// 成功
                //if (FujiCameraNative.CallXsdkDetect(out int plCount) == (int)FujiCameraNative.RESULT.XSDK_COMPLETE
                //    && plCount > 0)
                //{
                //    // 検出デバイスが1機以上ある場合
                //}

            }
            else
            {
                // 失敗
            }


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
            string objectName = string.Empty;
            string filePath = string.Empty;

            // ダイアログのインスタンスを生成
            var dialog = new OpenFileDialog();

            // TODO:ファイルの種類を設定
            //dialog.Filter = "";

            // ダイアログを表示する
            if(dialog.ShowDialog() == true)
            {
                // オブジェクト名を設定（yyyy/MM/dd/ファイル名）
                objectName = string.Format("{0}{1}", DateTime.Today.ToString(@"yyyy\/MM\/dd/"), dialog.SafeFileName);

                IsBusy = true;
                Progress = 0;

                // ファイルアップロード処理
                bool test = await _s3Bucket.UploadFileAsync(
                    SelectedBucket,
                    objectName,
                    dialog.FileName,
                    p => Progress = p);

                IsBusy = false;
                Progress = 0;

                // ファイル一覧の再読み込み
                LoadFilesAsync();
            }
        }

        private bool CanUpload()
        {
            // バケットが選択されている場合のみ実行可能
            return !string.IsNullOrEmpty(SelectedBucket);
        }

        /// <summary>
        /// ファイルダウンロード処理
        /// </summary>
        private async void Download()
        {
            //string bucketName = "";
            string objectName = string.Empty;

            //string folderPath = "D:\\repo\\S3\\download_Image";
            string folderPath = SelectDownloadFolder();
            if (folderPath == string.Empty)
            {
                return;
            }

            objectName = Files.Where(e => e.IsSelected).First().Key;

            IsBusy = true;
            Progress = 0;

            // ファイルのダウンロード
            bool result = await _s3Bucket.DownloadObjectFromBucketAsync(
                SelectedBucket,
                objectName,
                folderPath,
                p => Progress = p);

            IsBusy = false;
            Progress = 0;
        }

        /// <summary>
        /// ダウンロード先のフォルダ選択処理
        /// </summary>
        /// <returns>選択したフォルダパスを返却</returns>
        private string SelectDownloadFolder()
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Select folder to open",
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles)
            };

            string folderPath = string.Empty;
            if(dialog.ShowDialog() == true)
            {
                folderPath = dialog.FolderName;
            }

            return folderPath;
        }

        /// <summary>
        /// 選択したオブジェクトの削除処理
        /// </summary>
        private async void DeleteObjects()
        {
            foreach (var objectInfo in Files.Where(e => e.IsSelected))
            {
                // 選択したオブジェクトの削除
                await _s3Bucket.CreateAndDeleteObjectVersionAsync(SelectedBucket, objectInfo.Key);
            }

            // ファイル一覧の再読み込み
            LoadFilesAsync();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
