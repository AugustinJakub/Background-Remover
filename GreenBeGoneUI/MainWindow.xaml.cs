/*
 * Project: Green Screen - Chromakey Processing (UI)
 * Topic: Main Window Controller
 * Description: Handles custom window chrome interactions (Drag, Minimize, Close).
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

using System.Windows;
using System.Windows.Input;

namespace GreenBeGoneUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Start on the Landing Page
            MainFrame.Navigate(new LandingPage());
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}