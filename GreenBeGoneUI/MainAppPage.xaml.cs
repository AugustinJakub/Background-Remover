using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace GreenBeGoneUI
{
    public partial class MainAppPage : Page
    {
        // _baseImage: The pristine, original loaded image. NEVER MODIFIED directly.
        // This ensures we can re-run the algorithm with different parameters without reloading.
        private WriteableBitmap? _baseImage;

        // _displayBitmap: The working copy shown in the UI. 
        // Re-created from _baseImage for every process run.
        private WriteableBitmap? _displayBitmap;

        // Key color components selected by the user
        private int _keyR = 0;
        private int _keyG = 255;
        private int _keyB = 0;

        public MainAppPage()
        {
            InitializeComponent();

            // Inicjalizacja slidera liczbą wątków procesora
            InitializeThreadSlider();
        }

        /// <summary>
        /// Ustawia wartość slidera na liczbę dostępnych wątków procesora
        /// </summary>
        private void InitializeThreadSlider()
        {
            int processorCount = Environment.ProcessorCount;

            if (SliderThreadCount != null)
            {
                SliderThreadCount.Value = processorCount;
                SliderThreadCount.Maximum = Math.Max(processorCount, 16); // Minimum 16 jako max
            }
        }

        // Logic for switching tabs/views in the UI
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (GridSource == null) return;

            GridSource.Visibility = Visibility.Hidden;
            GridKeying.Visibility = Visibility.Hidden;
            GridPerformance.Visibility = Visibility.Hidden;
            GridPreview.Visibility = Visibility.Hidden;
            GridExport.Visibility = Visibility.Hidden;

            if (TabSource.IsChecked == true) GridSource.Visibility = Visibility.Visible;
            else if (TabKeying.IsChecked == true) GridKeying.Visibility = Visibility.Visible;
            else if (TabPerformance.IsChecked == true) GridPerformance.Visibility = Visibility.Visible;
            else if (TabPreview.IsChecked == true) GridPreview.Visibility = Visibility.Visible;
            else if (TabExport.IsChecked == true) GridExport.Visibility = Visibility.Visible;
        }

        // Handles loading an image from disk
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Image Files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All files (*.*)|*.*";

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage tempBitmap = new BitmapImage(new Uri(openDialog.FileName));

                    // Convert to BGRA32 format to ensure compatibility with our C++/ASM logic
                    FormatConvertedBitmap converter = new FormatConvertedBitmap();
                    converter.BeginInit();
                    converter.Source = tempBitmap;
                    converter.DestinationFormat = PixelFormats.Bgra32;
                    converter.EndInit();

                    // Load the immutable base image
                    _baseImage = new WriteableBitmap(converter);

                    // Create the initial display copy
                    _displayBitmap = new WriteableBitmap(_baseImage);

                    ProcessedImage.Source = _displayBitmap;
                    ImgColorPicker.Source = _displayBitmap;

                    TxtStatus.Text = $"> IMAGE LOADED:\n{Path.GetFileName(openDialog.FileName)}";

                    // Default to Green
                    UpdateKeyColor(0, 255, 0);

                    // AUTO-NAVIGATE: Go to Keying Tab (Color Picker)
                    TabKeying.IsChecked = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error");
                }
            }
        }

        private void UpdateKeyColor(byte r, byte g, byte b)
        {
            _keyR = r;
            _keyG = g;
            _keyB = b;
            if (ColorPreviewBorder != null)
                ColorPreviewBorder.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
            if (TxtKeyColorRGB != null)
                TxtKeyColorRGB.Text = $"R:{r} G:{g} B:{b}";
        }

        // Logic for picking a color directly from the image
        private void ImgColorPicker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Pick color from the BASE image to ensure we get the original pixel values
            if (_baseImage == null) return;

            Point pos = e.GetPosition(ImgColorPicker);
            double actualWidth = ImgColorPicker.ActualWidth;
            double actualHeight = ImgColorPicker.ActualHeight;

            // Calculate scale ratios to map click coordinates to pixel coordinates
            double imgRatio = (double)_baseImage.PixelWidth / _baseImage.PixelHeight;
            double ctrlRatio = actualWidth / actualHeight;

            double renderedWidth, renderedHeight, offsetX, offsetY;

            if (imgRatio > ctrlRatio)
            {
                renderedWidth = actualWidth;
                renderedHeight = actualWidth / imgRatio;
                offsetX = 0;
                offsetY = (actualHeight - renderedHeight) / 2;
            }
            else
            {
                renderedHeight = actualHeight;
                renderedWidth = actualHeight * imgRatio;
                offsetY = 0;
                offsetX = (actualWidth - renderedWidth) / 2;
            }

            double relativeX = pos.X - offsetX;
            double relativeY = pos.Y - offsetY;

            if (relativeX < 0 || relativeX >= renderedWidth || relativeY < 0 || relativeY >= renderedHeight)
                return;

            int x = (int)(relativeX * (_baseImage.PixelWidth / renderedWidth));
            int y = (int)(relativeY * (_baseImage.PixelHeight / renderedHeight));

            x = Math.Max(0, Math.Min(x, _baseImage.PixelWidth - 1));
            y = Math.Max(0, Math.Min(y, _baseImage.PixelHeight - 1));

            try
            {
                _baseImage.Lock();
                unsafe
                {
                    IntPtr pBackBuffer = _baseImage.BackBuffer;
                    int stride = _baseImage.BackBufferStride;
                    byte* pPixels = (byte*)pBackBuffer;

                    int index = y * stride + x * 4;

                    byte b = pPixels[index];
                    byte g = pPixels[index + 1];
                    byte r = pPixels[index + 2];

                    UpdateKeyColor(r, g, b);
                }
            }
            finally
            {
                _baseImage.Unlock();
            }
        }

        // Main processing trigger
        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            RunProcessing();

            TabPreview.IsChecked = true;
            if (GridPerformance != null) GridPerformance.Visibility = Visibility.Hidden;
            if (GridPreview != null) GridPreview.Visibility = Visibility.Visible;
        }

        private void RunProcessing()
        {
            if (_baseImage == null) return;

            // STATE FIX: Always create a fresh working copy from the pristine base image.
            // This satisfies the requirement to re-run algorithm without reloading file.
            _displayBitmap = new WriteableBitmap(_baseImage);

            ProcessedImage.Source = _displayBitmap;
            ImgColorPicker.Source = _displayBitmap;

            // Get parameters from UI
            int threadCount = (int)SliderThreadCount.Value;

            // Input Validation for Tolerance
            if (!int.TryParse(TxtTolerance.Text, out int tolerance))
                tolerance = 50;

            tolerance = Math.Max(0, Math.Min(255, tolerance));

            // Check which library to use (Dynamic Library Switching)
            bool useAsm = RbAsm.IsChecked == true;

            Stopwatch sw = Stopwatch.StartNew();

            _displayBitmap.Lock();
            try
            {
                IntPtr dataPtr = _displayBitmap.BackBuffer;

                // Dynamically call the appropriate DLL function
                if (useAsm)
                {
                    NativeProcessor.ProcessImage_ASM(
                        dataPtr,
                        _displayBitmap.PixelWidth,
                        _displayBitmap.PixelHeight,
                        _keyR, _keyG, _keyB,
                        tolerance,
                        threadCount
                    );
                }
                else
                {
                    NativeProcessor.ProcessImage_CPP(
                        dataPtr,
                        _displayBitmap.PixelWidth,
                        _displayBitmap.PixelHeight,
                        _keyR, _keyG, _keyB,
                        tolerance,
                        threadCount
                    );
                }

                _displayBitmap.AddDirtyRect(new Int32Rect(0, 0, _displayBitmap.PixelWidth, _displayBitmap.PixelHeight));
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                sw.Stop();
                _displayBitmap.Unlock();
            }

            // Display execution time (Requirement: Status line/Time indicator)
            TxtExecutionTime.Text = $"LAST RUN: {sw.Elapsed.TotalSeconds:F3} s";
            TxtStatus.Text = $"> PROCESSED:\n{(useAsm ? "ASM" : "C++")} | {sw.Elapsed.TotalSeconds:F3}s";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_displayBitmap == null)
            {
                MessageBox.Show("No image to save!", "Error");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PNG Image (*.png)|*.png";
            saveDialog.FileName = "Result.png";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(_displayBitmap));
                        encoder.Save(stream);
                    }
                    TxtStatus.Text = $"> SAVED:\n{Path.GetFileName(saveDialog.FileName)}";

                    // Show Custom Overlay
                    if (OverlaySuccess != null)
                        OverlaySuccess.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving image: {ex.Message}", "Error");
                }
            }
        }

        private void BtnCloseOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (OverlaySuccess != null)
                OverlaySuccess.Visibility = Visibility.Collapsed;
        }
    }
}