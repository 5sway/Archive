using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArchiveApp
{
    /// <summary>
    /// Логика взаимодействия для DocumentPage.xaml
    /// </summary>
    public partial class DocumentPage : Page
    {
        public DocumentPage()
        {
            InitializeComponent();
            LoadData();
        }
        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                DataGridTable.ItemsSource = context.Document.ToList();
            }
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTable.SelectedItem is Document selectedDoc)
            {
                using (var context = new ArchiveBaseEntities())
                {
                    var docToRemove = context.Document.Find(selectedDoc.Id);
                    if (docToRemove != null)
                    {
                        context.Document.Remove(docToRemove);
                        context.SaveChanges();
                    }
                }

                LoadData();
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTable.IsReadOnly)
            {
                DataGridTable.IsReadOnly = false;
                EditBtn.Content = "Сохранить";
            }
            else
            {
                DataGridTable.IsReadOnly = true;
                EditBtn.Content = "Изменить";
                SaveChanges();
            }
        }
        private void SaveChanges()
        {
            using (var context = new ArchiveBaseEntities())
            {
                foreach (var item in DataGridTable.Items)
                {
                    if (item is Document doc)
                    {
                        var docToUpdate = context.Document.Find(doc.Id);
                        if (docToUpdate != null)
                        {
                            docToUpdate.Number = doc.Number;
                            docToUpdate.Receipt_Date = doc.Receipt_Date;
                            docToUpdate.Title = doc.Title;
                            docToUpdate.Annotation = doc.Annotation;
                            docToUpdate.Source = doc.Source;
                            docToUpdate.Copies_Count = doc.Copies_Count;
                            docToUpdate.Storage_Type = doc.Storage_Type;
                        }
                    }
                }
                context.SaveChanges();
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
