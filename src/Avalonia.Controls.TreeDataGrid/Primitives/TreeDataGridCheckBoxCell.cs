using System;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;

namespace Avalonia.Controls.Primitives
{
    public class TreeDataGridCheckBoxCell : TreeDataGridEditableCell
    {
        public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool> IsThreeStateProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool>(
                nameof(IsThreeState),
                o => o.IsThreeState,
                (o, v) => o.IsThreeState = v);

        public static readonly DirectProperty<TreeDataGridCheckBoxCell, bool?> ValueProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridCheckBoxCell, bool?>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);

        private bool _isThreeState;
        private bool? _value;

        public bool IsThreeState
        {
            get => _isThreeState;
            set => SetAndRaise(IsThreeStateProperty, ref _isThreeState, value);
        }

        public bool? Value
        {
            get => _value;
            set
            {
                if (SetAndRaise(ValueProperty, ref _value, value))
                {
                    if (Model is CheckBoxCell cell)
                        cell.Value = value;
                    RaiseCellValueChanged();
                }
            }
        }

        public override void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection,
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            if (model is CheckBoxCell cell)
            {
                IsReadOnly = cell.IsReadOnly;
                IsThreeState = cell.IsThreeState;
                Value = cell.Value;
            }
            else
            {
                throw new InvalidOperationException("Invalid cell model.");
            }

            base.Realize(factory, selection, model, columnIndex, rowIndex);
        }

        protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName == nameof(CheckBoxCell.Value) && Model is CheckBoxCell checkBoxCell)
                Value = checkBoxCell.Value;
        }
    }
}
