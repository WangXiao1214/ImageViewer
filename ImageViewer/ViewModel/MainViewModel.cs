using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageViewer.ViewModel
{

using System.Windows.Media.Imaging;

public class ImageThumbnail : INotifyPropertyChanged
{
    private string _filePath;
    private string _fileName;
    private BitmapImage _thumbnailImage;
    
    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            OnPropertyChanged();
            LoadThumbnail(); // 加载缩略图
        }
    }
    
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }
    
    public BitmapImage ThumbnailImage
    {
        get => _thumbnailImage;
        set
        {
            _thumbnailImage = value;
            OnPropertyChanged();
        }
    }
    
    private void LoadThumbnail()
    {
        if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_filePath);
                bitmap.DecodePixelWidth = 100; // 设置缩略图宽度，提高性能
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // 提高性能
                ThumbnailImage = bitmap;
            }
            catch
            {
                ThumbnailImage = null;
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

    public class MainViewModel : INotifyPropertyChanged
    {
        private string _currentImagePath;
        private double _zoomScale = 1.0;
        private double _zoomPercent = 100;
        private double _rotationAngle = 0;
        private string _currentFileName;
        private string _imageDimensions;
        private string _fileSize;
        private ImageThumbnail _selectedThumbnail;

        public ObservableCollection<ImageThumbnail> ImageThumbnails { get; set; }

        public MainViewModel()
        {
            ImageThumbnails = new ObservableCollection<ImageThumbnail>();

            // 命令初始化
            OpenFileCommand = new RelayCommand(OpenFile);
            PreviousImageCommand = new RelayCommand(PreviousImage, CanNavigate);
            NextImageCommand = new RelayCommand(NextImage, CanNavigate);
            ZoomInCommand = new RelayCommand(ZoomIn);
            ZoomOutCommand = new RelayCommand(ZoomOut);
            RotateLeftCommand = new RelayCommand(RotateLeft);
            RotateRightCommand = new RelayCommand(RotateRight);
            DeleteImageCommand = new RelayCommand(DeleteImage);
            FullScreenCommand = new RelayCommand(ToggleFullScreen);
            ActualSizeCommand = new RelayCommand(SetActualSize);
            FitToWindowCommand = new RelayCommand(FitToWindow);
            ExitCommand = new RelayCommand(Exit);
            AboutCommand = new RelayCommand(ShowAbout);

        }

        public string CurrentImagePath
        {
            get { return _currentImagePath; }
            set
            {
                _currentImagePath = value;
                OnPropertyChanged();
                UpdateImageInfo();
            }
        }

        public double ZoomScale
        {
            get => _zoomPercent / 100;
        }

        public double ZoomPercent
        {
            get => _zoomPercent;
            set
            {
                _zoomPercent = Math.Max(1, Math.Min(400, value));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomScale));
            }
        }

        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                _rotationAngle = value % 360;
                OnPropertyChanged();
            }
        }

        public string CurrentFileName
        {
            get => _currentFileName;
            set
            {
                _currentFileName = value;
                OnPropertyChanged();
            }
        }

        public string ImageDimensions
        {
            get => _imageDimensions;
            set
            {
                _imageDimensions = value;
                OnPropertyChanged();
            }
        }

        public string FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged();
            }
        }

        public ImageThumbnail SelectedThumbnail
        {
            get => _selectedThumbnail;
            set
            {
                _selectedThumbnail = value;
                OnPropertyChanged();

                // 当选中缩略图时，打开对应的图片
                if (value != null && !string.IsNullOrEmpty(value.FilePath))
                {
                    CurrentImagePath = value.FilePath;
                }
            }
        }

        private void UpdateImageInfo()
        {
            if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
            {
                CurrentFileName = Path.GetFileName(_currentImagePath);

                try
                {
                    // 获取图片尺寸
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_currentImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImageDimensions = $"{bitmap.PixelWidth} x {bitmap.PixelHeight} px";
                }
                catch (Exception ex)
                {
                    ImageDimensions = "无法读取";
                    System.Diagnostics.Debug.WriteLine($"读取图片尺寸失败: {ex.Message}");
                }

                try
                {
                    // 获取文件大小
                    var fileInfo = new FileInfo(_currentImagePath);
                    long length = fileInfo.Length;
                    if (length < 1024)
                        FileSize = $"{length} B";
                    else if (length < 1024 * 1024)
                        FileSize = $"{length / 1024.0:F1} KB";
                    else
                        FileSize = $"{length / 1024.0 / 1024.0:F1} MB";
                }
                catch (Exception ex)
                {
                    FileSize = "未知";
                    System.Diagnostics.Debug.WriteLine($"读取文件大小失败: {ex.Message}");
                }
            }
            else
            {
                CurrentFileName = "无图片";
                ImageDimensions = "0 x 0 px";
                FileSize = "0 B";
            }
        }

        private void LoadImagesFromFolder(string folderPath)
        {
            ImageThumbnails.Clear();

            try
            {
                var imageFiles = Directory.GetFiles(folderPath, "*.*")
                                .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") ||
                                           f.EndsWith(".png") || f.EndsWith(".bmp") ||
                                           f.EndsWith(".gif"))
                                .ToList();

                foreach (var file in imageFiles)
                {
                    ImageThumbnails.Add(new ImageThumbnail
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file)
                    });
                }
            }
            catch
            {
                ImageThumbnails.Clear ();
            }


        }

        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "选择图片"
            };

            if(openFileDialog.ShowDialog() == true)
            {
                CurrentImagePath = openFileDialog.FileName;
                var directory = Path.GetDirectoryName(CurrentImagePath);
                LoadImagesFromFolder(directory);

                ZoomPercent = 100;
                RotationAngle = 0;
            }
        }

        private void PreviousImage()
        {
            if (ImageThumbnails.Count == 0) return;

            if (ImageThumbnails.Count == 1)
            {
                // 只有一张图片，不进行操作
                return;
            }

            var currentIndex = ImageThumbnails.IndexOf(
                ImageThumbnails.FirstOrDefault(t => t.FilePath == CurrentImagePath));

            if (currentIndex >= 0)
            {
                var prevIndex = (currentIndex - 1 + ImageThumbnails.Count) % ImageThumbnails.Count;
                CurrentImagePath = ImageThumbnails[prevIndex].FilePath;
            }
        }

        private void NextImage()
        {
            if (ImageThumbnails.Count == 0) return;

            if (ImageThumbnails.Count == 1)
            {
                // 只有一张图片，不进行操作
                return;
            }

            var currentIndex = ImageThumbnails.IndexOf(
                ImageThumbnails.FirstOrDefault(t => t.FilePath == CurrentImagePath));

            if (currentIndex >= 0)
            {
                var nextIndex = (currentIndex + 1) % ImageThumbnails.Count;
                CurrentImagePath = ImageThumbnails[nextIndex].FilePath;
            }
        }

        private void ZoomIn() => ZoomPercent += 10;
        private void ZoomOut() => ZoomPercent -= 10;
        private void RotateLeft() => RotationAngle -= 90;
        private void RotateRight() => RotationAngle += 90;
        private void SetActualSize() => ZoomPercent = 100;
        private void FitToWindow() => ZoomPercent = 100; // 需要根据窗口大小计算


        private void DeleteImage()
        {
            if (!string.IsNullOrEmpty(CurrentImagePath) && File.Exists(CurrentImagePath))
            {
                if (System.Windows.MessageBox.Show("确定要删除这张图片吗？", "确认删除",
                    System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                {
                    var dir = Path.GetDirectoryName(CurrentImagePath);
                    File.Delete(CurrentImagePath);
                    NextImage();
                    LoadImagesFromFolder(dir);
                }
            }
        }



        private void ToggleFullScreen() { /* 实现全屏逻辑 */ }
        private void Exit() => System.Windows.Application.Current.Shutdown();
        private void ShowAbout() { /* 显示关于对话框 */ }
        private bool CanNavigate() => ImageThumbnails.Count > 0;

        // 命令属性
        public ICommand OpenFileCommand { get; }
        public ICommand PreviousImageCommand { get; }
        public ICommand NextImageCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand RotateLeftCommand { get; }
        public ICommand RotateRightCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand FullScreenCommand { get; }
        public ICommand ActualSizeCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
            public void Execute(object parameter) => _execute();
            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }

    }

}
