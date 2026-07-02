/*
 * Project: Green Screen - Chromakey Processing (UI)
 * Topic: Landing Page Logic
 * Description: Handles the "Slide to Start" interaction logic.
 * Date: Winter Semester 2025/2026
 * Author: Jakub Augustin
 * Version: 1.0
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace GreenBeGoneUI
{
    public partial class LandingPage : Page
    {
        public LandingPage()
        {
            InitializeComponent();
            StartSlider.PreviewMouseLeftButtonUp += StartSlider_PreviewMouseLeftButtonUp;
        }

        private void StartSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do not navigate here. Navigation is handled on mouse lift to prevent accidental triggers.
        }

        private void StartSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Threshold set to 90 for better UX
            if (StartSlider.Value >= 90)
            {
                if (NavigationService != null)
                {
                    NavigationService.Navigate(new MainAppPage());
                }
            }
            // Always reset slider value after interaction
            StartSlider.Value = 0;
        }
    }
}