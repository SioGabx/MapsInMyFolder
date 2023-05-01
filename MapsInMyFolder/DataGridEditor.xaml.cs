using MapsInMyFolder.Commun;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour DataGridEditor.xaml
    /// </summary>
    public partial class DataGridEditor : Window
    {
        private static ObservableCollection<Layers> _items;
        public DataGridEditor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TitleTextBox.Text = this.Title = "MapsInMyFolder";
            GetData();
        }


        private void GetData()
        {
            _items = new ObservableCollection<Layers>();

            foreach (Layers layers in Layers.GetLayersList())
            {
                _items.Add(layers);
            }
            dataGrid.ItemsSource = _items;
        }

        private void dataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            // Obtenir la valeur de la cellule modifiée
            string cellValue = "NULL";
            Type CellDataType = e.EditingElement.GetType();

            if (CellDataType == typeof(TextBox))
            {
                cellValue = (e.EditingElement as TextBox).Text;
            }
            else if (CellDataType == typeof(CheckBox))
            {
                cellValue = (e.EditingElement as CheckBox).IsChecked.ToString();
            }
            if (cellValue != "NULL")
            {
                cellValue = "'" + Collectif.HTMLEntities(cellValue) + "'";
            }


            // Obtenir la ligne correspondante à la cellule modifiée
            string editedCollumnName = e.Column.Header.ToString();

            // Obtenir la valeur de la première colonne de la ligne modifiée
            var editedRow = e.Row;
            var firstColumnValue = editedRow.Item as Layers;
            int LayerId = firstColumnValue.class_id;

            string DatabaseSQLCommands = "";
            if (Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'EDITEDLAYERS' WHERE ID = " + LayerId) == 0)
            {
                DatabaseSQLCommands = $"INSERT INTO 'main'.'EDITEDLAYERS'('ID') VALUES ('{LayerId}');";
            }
            DatabaseSQLCommands += $"UPDATE 'main'.'EDITEDLAYERS' SET '{editedCollumnName}'={cellValue} WHERE ID = {LayerId};";
            Database.ExecuteNonQuerySQLCommand(DatabaseSQLCommands);


        }

        private void dataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Si la touche CTRL est enfoncée, défilez horizontalement
                var scrollViewer = Collectif.FindChild<ScrollViewer>(dataGrid);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - (e.Delta / 1.5));
                    e.Handled = true;
                }
            }
        }

        private void dataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            //todo : add button to add new row + paste support
            Debug.WriteLine("Added");
        }
    }

}
