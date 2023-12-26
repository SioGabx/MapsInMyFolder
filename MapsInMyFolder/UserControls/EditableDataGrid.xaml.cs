using ICSharpCode.AvalonEdit.Highlighting;
using MapsInMyFolder.Commun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateCellEditor();
        }

        private void UpdateCellEditor()
        {
            if (dataGrid.SelectedCells.Count > 1)
            {
                return;
            }
            if (dataGrid?.CurrentItem is not Layers SelectedLayer)
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
            SwitchNextCell(newEventArgs);
            //TextBoxEditor.CaretIndex = TextBoxEditor.Text.Length;
        }


        private void SwitchNextCell(KeyEventArgs newEventArgs)
        {
            FocusCurrentSelectedCell();
            dataGrid.RaiseEvent(newEventArgs);
            //element.Focus();
        }



        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!isCellBeingEdited)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (e.Key == Key.C)
                    {
                        CopySelectedCells();
                        e.Handled = true;
                    }
                    if (e.Key == Key.V)
                    {
                        this.Cursor = Cursors.Wait;
                        PasteIntoCells();
                        this.Cursor = Cursors.Arrow;
                        e.Handled = true;
                    }
                }
            }
        }


        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            isCellBeingEdited = true;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
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

        private bool IsContiguousSelection(DataGrid dataGrid)
        {
            List<DataGridCellInfo> SelectedCells = dataGrid.SelectedCells.ToList();
            DataGridCellInfo currentCell = SelectedCells.FirstOrDefault(dataGrid.CurrentCell);
            dataGrid.CurrentCell = currentCell;
            dataGrid.BeginEdit();
            // The index of the first DataGridRow
            if (dataGrid?.ItemContainerGenerator?.ContainerFromItem(currentCell.Item) is not DataGridRow Container)
            {
                return true;
            }
            int? startRow = dataGrid?.ItemContainerGenerator?.IndexFromContainer(Container);
            if (startRow == null) { return true; } 
            int NumberOfColumnsSelected = SelectedCells.Select(s => s.Column).Distinct().Count();
            int NumberOfRowSelected = SelectedCells.Select(s => s.Item).Distinct().Count();

            // The destination rows 
            // (from startRow to either end or length of clipboard rows)
            DataGridRow[] rows =
                Enumerable.Range(
                    (int)startRow, Math.Min(dataGrid.Items.Count, NumberOfRowSelected))
                .Select(rowIndex =>
                    dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow)
                .Where(a => a != null).ToArray();

            // The destination columns 
            // (from selected row to either end or max. length of clipboard columns)
            DataGridColumn[] columns =
                dataGrid.Columns.OrderBy(column => column.DisplayIndex)
                .SkipWhile(column => column != currentCell.Column)
                .Take(NumberOfColumnsSelected).ToArray();

            for (int selectedRowIndex = 0; selectedRowIndex < NumberOfRowSelected; selectedRowIndex++)
            {
                DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex((int)startRow + selectedRowIndex) as DataGridRow;
                if (row is null)
                {
                    continue;
                }
                for (int colIndex = 0; colIndex < NumberOfColumnsSelected; colIndex++)
                {
                    DataGridCellInfo newCell = new DataGridCellInfo(row.Item, columns[colIndex]);
                    if (!dataGrid.SelectedCells.Contains(newCell))
                    {
                        return false;
                    }

                }
            }
            return true;
        }

        private async void PasteIntoCells()
        {
            if (!IsContiguousSelection(dataGrid))
            {
                try { 
                await Message.SetContentDialog(Languages.Current["EditableDataGridImpossibleActionOnMultiplePlage"], Languages.Current["dialogTitleOperationFailed"], MessageDialogButton.OK).ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
                return;
            }
            string data = Clipboard.GetText();
            string[][] clipboardData = Collectif.ParseCSV(data);
            List<DataGridCellInfo> SelectedCells = dataGrid.SelectedCells.ToList();
            DataGridCellInfo currentCell = SelectedCells.FirstOrDefault(dataGrid.CurrentCell);


            int NumberOfColumnsSelected = SelectedCells.Select(s => s.Column).Distinct().Count();
            int NumberOfRowSelected = SelectedCells.Select(s => s.Item).Distinct().Count();

            // The index of the first DataGridRow
            if (dataGrid?.ItemContainerGenerator?.ContainerFromItem(currentCell.Item) is not DataGridRow Container)
            {
                return;
            }
            int? startRow = dataGrid?.ItemContainerGenerator?.IndexFromContainer(Container);
            if (startRow == null) { return; }

            // The destination rows 
            // (from startRow to either end or length of clipboard rows)
            DataGridRow[] rows =
                Enumerable.Range(
                    (int)startRow, Math.Min(dataGrid.Items.Count, clipboardData.Length))
                .Select(rowIndex =>
                    dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow)
                .Where(a => a != null).ToArray();

            // The destination columns 
            // (from selected row to either end or max. length of clipboard columns)
            DataGridColumn[] columns =
                dataGrid.Columns.OrderBy(column => column.DisplayIndex)
                .SkipWhile(column => column != currentCell.Column)
                .Take(clipboardData.Max(row => row.Length)).ToArray();

            DataGridColumn[] SelectedColumns =
                dataGrid.Columns.OrderBy(column => column.DisplayIndex)
                .SkipWhile(column => column != currentCell.Column)
                .Take(clipboardData.Max(row => NumberOfColumnsSelected)).ToArray();

            //if (rows.Count() > NumberOfRowSelected && columns.Count() > NumberOfColumnsSelected)
            //{
                // Clear the current selection
                dataGrid.SelectedCells.Clear();
            //}
            // Adjust the selection to accommodate the clipboard content
            int rowCountToSelect = Math.Max(rows.Length, NumberOfRowSelected);
            int colCountToSelect = Math.Max(columns.Length, NumberOfColumnsSelected);

            for (int selectedRowIndex = 0; selectedRowIndex < rowCountToSelect; selectedRowIndex++)
            {
                DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex((int)startRow + selectedRowIndex) as DataGridRow;
                string[] rowContent = clipboardData[selectedRowIndex % clipboardData.Length];

                for (int colIndex = 0; colIndex < colCountToSelect; colIndex++)
                {
                    string cellContent = rowContent[colIndex % columns.Length];
                    DataGridColumn column = null;
                    if ((SelectedColumns.Count()) > colIndex)
                    {
                        //Selected Columns is greater than selection
                        column = SelectedColumns[colIndex] as DataGridColumn;
                    }
                    else
                    {
                        column = columns[colIndex % columns.Length];
                    }

                    column.OnPastingCellClipboardContent(row.Item, cellContent);

                    // Select the pasted cell
                    DataGridCellInfo newCell = new DataGridCellInfo(row.Item, column);
                    if (!dataGrid.SelectedCells.Contains(newCell))
                    {
                        dataGrid.SelectedCells.Add(newCell);
                    }

                }
            }
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
