﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.ComponentModel;

using TAlex.PowerCalc.Locators;
using TAlex.PowerCalc.Helpers;
using TAlex.PowerCalc.ViewModels;
using TAlex.PowerCalc.ViewModels.WorksheetMatrix;


namespace TAlex.PowerCalc.Controls
{
    /// <summary>
    /// Interaction logic for WorksheetMatrix.xaml
    /// </summary>
    public partial class WorksheetMatrix : UserControl
    {
        #region Fields

        private static readonly int DefaultColumnCount = 256;
        private static readonly int DefaultRowCount = 256;

        private bool _formulaBarEdit = false;
        private DataGridCell _lastEditedCellViaFormulaBar = null;

        #endregion

        #region Properties

        public TextBox FormulaBar
        {
            get
            {
                return formulaBarTextBox;
            }
        }

        public ContextMenu FormulaBarContextMenu
        {
            get
            {
                return formulaBarTextBox.ContextMenu;
            }

            set
            {
                formulaBarTextBox.ContextMenu = value;
            }
        }

        #endregion

        #region Constructors

        public WorksheetMatrix()
        {
            InitializeComponent();

            Initialize();
        }

        #endregion

        #region Methods

        public bool CommitEdit()
        {
            return dataGrid.CommitEdit();
        }

        public void Initialize()
        {
            int colCount = DefaultColumnCount;
            int rowCount = DefaultRowCount;

            dataGrid.Columns.Clear();
            for (int i = 0; i < colCount; i++)
            {
                dataGrid.Columns.Add(new DataGridTextColumnEx
                {
                    Header = Helpers.A1ReferenceHelper.IntegerToA1ReferenceColumn(i),
                    Binding = new Binding(String.Format("[{0}].FormattedValue", i)) { ValidatesOnExceptions = true, NotifyOnValidationError = true },
                    EditingBinding = new Binding(String.Format("[{0}].Expression", i)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }
                });
            }

            DataTable dataTable = new DataTable(((WorksheetMatrixViewModel)DataContext).ExpressionTreeBuilder);

            for (int i = 0; i < rowCount; i++)
            {
                dataTable.Rows.Add(new DataRow(dataTable, colCount) { RowNumber = i + 1 });
            }

            dataGrid.ItemsSource = dataTable.Rows;
        }

        #region Event Handlers

        /// <summary>
        /// Auto switching to edit mode after typing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (DataGridHelper.HasNonEscapeCharacters(e))
            {
                ((DataGrid)sender).BeginEdit(e);
            }
        }

        /// <summary>
        /// Set binding (Expression property) between formula bar text box and selected cell.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGrid.CurrentCell.Column != null)
            {
                formulaBarTextBox.SetBinding(TextBox.TextProperty, new Binding("Expression") { Source = GetDataCell(dataGrid.CurrentCell), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
        }

        /// <summary>
        /// 1. Commit editing if last editing was via formula bar
        /// 2. Display cell range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (_lastEditedCellViaFormulaBar != null && _lastEditedCellViaFormulaBar != dataGrid.TryToFindGridCell(dataGrid.CurrentItem, dataGrid.CurrentColumn))
            {
                _lastEditedCellViaFormulaBar.IsEditing = false;
                _lastEditedCellViaFormulaBar = null;
            }
            
            int selectedCells = dataGrid.SelectedCells.Count;
            if (selectedCells > 0)
            {
                DataGridCellInfo firstCellInfo = dataGrid.SelectedCells[0];

                if (selectedCells == 1)
                {
                    nameBoxTextBox.Text = Helpers.A1ReferenceHelper.ToString(firstCellInfo.Column.DisplayIndex, dataGrid.Items.IndexOf(firstCellInfo.Item) + 1);
                }
                else
                {
                    DataGridCellInfo lastCellInfo = dataGrid.SelectedCells[selectedCells - 1];

                    nameBoxTextBox.Text = Helpers.A1ReferenceHelper.ToString(firstCellInfo.Column.DisplayIndex, dataGrid.Items.IndexOf(firstCellInfo.Item) + 1,
                        lastCellInfo.Column.DisplayIndex, dataGrid.Items.IndexOf(lastCellInfo.Item) + 1);
                }
            }
        }

        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_formulaBarEdit)
            {
                e.Cancel = true;
                _formulaBarEdit = false;
                _lastEditedCellViaFormulaBar = dataGrid.TryToFindGridCell(e.Row.Item, e.Column);
            }

            //---------------------------------------------------------------
            var currentDataCell = GetDataCell(new DataGridCellInfo(e.Row.Item, e.Column));

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                DataGridCellInfo firstCell = dataGrid.SelectedCells[0];
                DataGridCellInfo lastCell = dataGrid.SelectedCells[dataGrid.SelectedCells.Count - 1];

                int firstSelectedItemIndex = dataGrid.Items.IndexOf(firstCell.Item);
                int lastSelectedItemIndex = dataGrid.Items.IndexOf(lastCell.Item);
                int firstSelectedColumnIndex = firstCell.Column.DisplayIndex;
                int lastSelectedColumnIndex = lastCell.Column.DisplayIndex;

                int rows = Math.Abs(firstSelectedItemIndex - lastSelectedItemIndex) + 1;
                int cols = Math.Abs(firstSelectedColumnIndex - lastSelectedColumnIndex) + 1;

                int rowOffset = Math.Min(firstSelectedItemIndex, lastSelectedItemIndex);
                int colOffset = Math.Min(firstSelectedColumnIndex, lastSelectedColumnIndex);

                //if (currentDataCell.Parent == null)
                    new DataArray(currentDataCell, colOffset, rowOffset, rows, cols);
                //else
                //    currentDataCell.Parent.Expand(colOffset, rowOffset, rows, cols);
            }
            else
            {
                if (!Keyboard.IsKeyDown(Key.Escape) && currentDataCell.Parent != null)
                {
                    MessageBox.Show("You can't edit part of array", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void dataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    OnDataGridDeleteKeyDown(e);
                    break;
            }
        }

        private void formulaBarTextBox_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool?)e.NewValue == true)
            {
                if (!dataGrid.SelectedCells.Any())
                {
                    SelectFirstCell();
                }

                _formulaBarEdit = true;
                dataGrid.BeginEdit();
                formulaBarTextBox.Focus();
            }
        }

        private void formulaBarTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    OnFormulaBarEnterKeyDown(e);
                    break;
            }
        }


        protected void OnDataGridDeleteKeyDown(KeyEventArgs e)
        {
            if (!dataGrid.GetCurrentDataGridCell().IsEditing)
            { 
                int selectedCells = dataGrid.SelectedCells.Count;

                for (int i = 0; i < selectedCells; i++)
                {
                    DataGridCellInfo cellInfo = dataGrid.SelectedCells[i];
                    DataCell dataCell = GetDataCell(cellInfo);
                    dataCell.Clear();
                }
            }
        }

        protected void OnFormulaBarEnterKeyDown(KeyEventArgs e)
        {
            if (dataGrid.SelectedCells.Any())
            {
                DataGridCell editingCell = dataGrid.GetSelectedDataCells().FirstOrDefault(x => x.IsEditing);

                if (editingCell != null)
                {
                    e.Handled = true;
                    editingCell.Focus();

                    var keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Return) { RoutedEvent = Keyboard.KeyDownEvent };
                    editingCell.RaiseEvent(keyEventArgs);
                }
            }
        }

        #endregion

        #region Helpers

        public DataCell GetDataCell(int column, int row)
        {
            return (dataGrid.Items[row] as DataRow)[column];
        }

        private DataCell GetDataCell(DataGridCellInfo cellInfo)
        {
            return (cellInfo.Item as DataRow)[cellInfo.Column.DisplayIndex];
        }

        private void SelectFirstCell()
        {
            DataGridCellInfo firstCellInfo = new DataGridCellInfo(dataGrid.Items[0], dataGrid.Columns[0]);

            dataGrid.UnselectAllCells();
            dataGrid.SelectedCells.Add(firstCellInfo);
            dataGrid.CurrentCell = firstCellInfo;
        }

        #endregion

        #endregion
    }
}
