using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;

namespace TOCTransfomer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Transformer transformer;

        public MainWindow()
        {
            InitializeComponent();
            transformer = new Transformer();
        }

        private async void  Btn_transform_Click(object sender, RoutedEventArgs e)
        {
            Reset();

            var fileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "CSV-Files|*.csv|All Files|*.*",
            };

            if ((bool)!fileDialog.ShowDialog())
                return;

            var file = fileDialog.FileName;
            try
            {
                UpdateProgress("CSV Lesen");

                transformer.ReadCSV(file);
                
                UpdateProgress("Schreibe TXT und CSV");

                await transformer.WriteTransformedCSV();

                UpdateProgress("Schreibe ATD");

                await transformer.WriteATD();

                UpdateProgress();
            }
            catch (Transformer.ReadingException)
            {
                info_box.Text = "Fehler beim lesen der csv.";
                info_box.Foreground = Brushes.Red;
                return;
            }
            catch (Transformer.ColumnException)
            {
                info_box.Text = "Fehler: Spalten nicht erkannt.";
                info_box.Foreground = Brushes.Red;
                return;
            }
            catch (Exception ex)
            {
                info_box.Text = "Unbekannter Fehler." +
                    "\r\n" +
                    "\r\n" +
                    $"{ex.Message}";
                info_box.Foreground = Brushes.Red;
                return;
            }

            info_box.Text = "Erfolgreich.";
            info_box.Foreground = Brushes.Green;
        }

        private void Reset()
        {
            Dispatcher.Invoke(() =>
            {
                progress_bar.Value = 0;
                info_box.Text = "Select a csv file to transform";
                info_box.Foreground = Brushes.Black;
            });
        }

        private void UpdateProgress(string text = "")
        {
            Dispatcher.Invoke(() =>
            {
                progress_bar.Value++;
                info_box.Text = text;
                info_box.Foreground = Brushes.Orange;
            });
        }

    }

}
