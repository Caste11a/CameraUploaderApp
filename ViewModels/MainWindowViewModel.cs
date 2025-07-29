using Amazon.S3.Model;
using CameraUploaderApp.Model;
using CommonPlatform.Native;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using S3Bucket = CameraUploaderApp.Services.S3Bucket;

namespace CameraUploaderApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> Buckets { get; set; } = new();
        public ObservableCollection<S3ObjectItem> Files { get; set; } = new();

        private RelayCommand _uploadCommand;
        private RelayCommand _downloadCommand;
        private RelayCommand _deleteCommand;
        private RelayCommand _nextPageCommand;

        public ICommand UploadCommand => _uploadCommand;
        public ICommand DownloadCommand => _downloadCommand;
        public ICommand DeleteCommand => _deleteCommand;
        public ICommand NextPageCommand => _nextPageCommand;

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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel() 
        {
            _s3Bucket = new S3Bucket();
            // イベント
            _uploadCommand = new RelayCommand(Upload, CanUpload);
            _downloadCommand = new RelayCommand(Download, IsSelectedFile);
            _deleteCommand = new RelayCommand(DeleteObjects, IsSelectedFile);

            // S3Objectitemの選択状態変更時に再評価
            S3ObjectItem.IsSelectedChanged += () =>
            {
                Application.Current.Dispatcher.Invoke(() => {
                    _downloadCommand?.RaiseCanExecuteChanged();
                    _deleteCommand?.RaiseCanExecuteChanged();
                });
            };

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

        /// <summary>
        /// 初期化処理
        /// </summary>
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
            List<S3Object> S3ObjectList = await _s3Bucket.GetFilesInBucketAsync(SelectedBucket);

            Files.Clear();
            foreach (var obj in S3ObjectList)
            {
                Files.Add(new S3ObjectItem{
                    Key = obj.Key,
                    IsSelected = false,
                    LastModified = obj.LastModified?.ToString("yyyy/MM/dd HH:mm"),
                    ObjectSize = obj.Size / 1024, // KBに変換
                });
            }
        }

        #region アップロード処理関連
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

#endregion

        #region ダウンロード処理関連
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

        private bool IsSelectedFile()
        {
            // バケットが選択されている場合のみ実行可能
            return Files.Any(e => e.IsSelected);
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

        #endregion

        #region 削除処理関連
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
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
