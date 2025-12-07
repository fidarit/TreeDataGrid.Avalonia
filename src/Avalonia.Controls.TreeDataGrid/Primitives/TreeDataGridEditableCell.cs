using System;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;

namespace Avalonia.Controls.Primitives
{
    public class TreeDataGridEditableCell : TreeDataGridCell
    {
        public static readonly DirectProperty<TreeDataGridEditableCell, bool> IsReadOnlyProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridEditableCell, bool>(
                nameof(IsReadOnly),
                o => o.IsReadOnly,
                (o, v) => o.IsReadOnly = v);

        private bool _isReadOnly;

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (SetAndRaise(IsReadOnlyProperty, ref _isReadOnly, value) && value && IsEditing)
                    CancelEdit();
            }
        }

        public override void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection,
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            if (model is IEditableCell cell)
            {
                IsReadOnly = cell.IsReadOnly;
            }
            else
            {
                throw new InvalidOperationException("Invalid cell model.");
            }

            base.Realize(factory, selection, model, columnIndex, rowIndex);
            SubscribeToModelChanges();
        }

        public override void Unrealize()
        {
            UnsubscribeFromModelChanges();
            base.Unrealize();
        }
        
        protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName == nameof(IEditableCell.IsReadOnly) && Model is IEditableCell cell)
                IsReadOnly = cell.IsReadOnly;
        }
    }
}
