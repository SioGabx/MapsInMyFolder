using ICSharpCode.AvalonEdit.Highlighting;
using MapsInMyFolder.Commun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour EditableDataGrid.xaml
    /// </summary>
    public partial class EditableDataGrid : UserControl
    {
        private bool isCellBeingEdited = false;
        public EditableDataGrid()
        {
            InitializeComponent();
        }


        public IEnumerable ItemsSource
        {
            get { return dataGrid.ItemsSource; }
            set { dataGrid.ItemsSource = value; }
        }


        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateCellEditor();
        }

        private string currentSavedCellValue = string.Empty;
        private void dataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateCellEditor();
        }

        private void UpdateCellEditor()
        {
            Layers SelectedLayer = dataGrid?.CurrentItem as Layers;
            if (SelectedLayer is null)
            {
                return;
            }
            BindingOperations.ClearBinding(TextBoxEditor, TextBox.TextProperty);
            BindingOperations.ClearBinding(CodeEditor, ScriptEditor.ScriptProperty);
            string BindingPath = (dataGrid.CurrentColumn as DataGridTextColumn)?.SortMemberPath;
            Binding binding = new Binding(BindingPath)
            {
                Source = SelectedLayer,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            currentSavedCellValue = SelectedLayer.GetPropertyValue(BindingPath).ToString();
            TextBoxEditor.Visibility = Visibility.Collapsed;
            TextBoxEditor.TextWrapping = TextWrapping.NoWrap;
            TextBoxEditor.AcceptsReturn = false;
            TextBoxEditor.AcceptsTab = false;
            CodeEditor.Visibility = Visibility.Collapsed;
            switch (BindingPath)
            {
                case "Name":
                case "Tag":
                case "Country":
                case "Identifier":
                case "TileUrl":
                case "SiteName":
                case "SiteUrl":
                case "MinZoom":
                case "MaxZoom":
                case "TilesFormat":
                case "TilesSize":
                case "Visibility":
                case "IsAtScale":
                    TextBoxEditor.Visibility = Visibility.Visible;
                    break;
                case "Description":
                    TextBoxEditor.Visibility = Visibility.Visible;
                    TextBoxEditor.TextWrapping = TextWrapping.Wrap;
                    TextBoxEditor.AcceptsReturn = true;
                    TextBoxEditor.AcceptsTab = true;
                    break;
                case "Style":
                case "SpecialsOptions":
                case "BoundaryRectangles":
                    CodeEditor.Visibility = Visibility.Visible;
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MIMF_Json");
                    break;
                case "Script":
                    CodeEditor.Visibility = Visibility.Visible;
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MIMF_JavaScript");
                    break;
            }

            if (TextBoxEditor.Visibility == Visibility.Visible)
            {
                TextBoxEditor.SetBinding(TextBox.TextProperty, binding);
            }
            else
            {
                CodeEditor.SetBinding(ScriptEditor.ScriptProperty, binding);
            }
        }


        private void TextBoxEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!TextBoxEditor.AcceptsReturn)
                {
                    SwithNextCellFromTextBoxEditor(e);
                }
            }
            if (e.Key == Key.Tab)
            {
                if (!TextBoxEditor.AcceptsTab)
                {
                    SwithNextCellFromTextBoxEditor(e);
                }
            }

            if (e.Key == Key.Escape)
            {
                TextBoxEditor.Text = currentSavedCellValue;
                SwithNextCellFromTextBoxEditor(e);
            }
        }

        private void SwithNextCellFromTextBoxEditor(KeyEventArgs e)
        {
            e.Handled = true;
            KeyEventArgs newEventArgs = new KeyEventArgs(e.KeyboardDevice, PresentationSource.FromVisual(dataGrid), e.Timestamp, e.Key)
            {
                RoutedEvent = UIElement.KeyDownEvent
            };
            SwitchNextCell(newEventArgs, TextBoxEditor);
            //TextBoxEditor.CaretIndex = TextBoxEditor.Text.Length;
        }


        private void SwitchNextCell(KeyEventArgs newEventArgs, UIElement element)
        {
            FocusCurrentSelectedCell();
            dataGrid.RaiseEvent(newEventArgs);
            //element.Focus();
        }



        private void dataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!isCellBeingEdited)
            {
                if (e.Key == Key.C && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    CopySelectedCells();
                    e.Handled = true;
                }

            }
        }


        private void dataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            isCellBeingEdited = true;
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            isCellBeingEdited = false;
        }

        private void FocusCurrentSelectedCell()
        {
            dataGrid.Focus();
            DataGridCellInfo? selectedCell = dataGrid.SelectedCells?.FirstOrDefault();
            if (selectedCell == null) { return; }
            DataGridCellInfo cell = (DataGridCellInfo)selectedCell;
            dataGrid.CurrentCell = cell;
            dataGrid.BeginEdit();
        }


        private void CopySelectedCells()
        {
            var selectedCells = dataGrid.SelectedCells;
            if (selectedCells.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                int currentRowIndex = -1;
                foreach (DataGridCellInfo cell in selectedCells)
                {
                    int rowIndex = dataGrid.Items.IndexOf(cell.Item);
                    if (rowIndex != currentRowIndex)
                    {
                        if (currentRowIndex != -1)
                        {
                            //first row, newline character to separate rows
                            sb.Append('\n');
                        }
                        currentRowIndex = rowIndex;
                    }
                    string RawValue = cell.GetCellValue();
                    string CellValue = '"' + RawValue.Replace("\"", "\"\"") + '"';
                    sb.Append(CellValue);
                    sb.Append('\t');
                }
                Clipboard.SetText(sb.ToString().Replace("\t\n", "\n").TrimEnd('\t'));
            }
        }

    }
}
