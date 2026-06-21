using System.Windows;
using System.Windows.Controls;

namespace ArchiveApp
{
    public partial class AuditPage : Page
    {
        public AuditPage()
        {
            InitializeComponent();
            LoadCurrentSession();
        }

        private void LoadFullHistory()
        {
            AuditDataGrid.ItemsSource = AuditService.GetFullHistory();
        }

        private void LoadCurrentSession()
        {
            AuditDataGrid.ItemsSource = AuditService.GetCurrentSessionLogs();
        }

        private void CurrentSession_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentSession();
        }

        private void FullHistory_Click(object sender, RoutedEventArgs e)
        {
            LoadFullHistory();
        }

        private void ClearSession_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить логи текущей сессии?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AuditService.ClearCurrentSession();
                LoadCurrentSession();
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить всю историю аудита? Это действие необратимо!", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                AuditService.ClearFullHistory();
                LoadFullHistory();
            }
        }
    }
}