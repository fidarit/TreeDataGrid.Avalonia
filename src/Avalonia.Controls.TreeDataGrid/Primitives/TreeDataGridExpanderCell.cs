using System;
using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control in a <see cref="TreeDataGrid" /> that displays a cell with an expander.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridExpanderCell is used for displaying hierarchical data in a TreeDataGrid. It
    ///     shows an  expander button that allows users to expand or collapse child rows, and
    ///     embeds another cell (the content cell) that displays the actual data value.
    ///   </para>
    ///   <para>
    ///     The expander cell is typically used in the first column of a hierarchical TreeDataGrid to
    ///     provide the tree structure visualization. It handles the indentation of rows based on their
    ///     hierarchy level and manages the expanded/collapsed state of tree nodes.
    ///   </para>
    /// </remarks>
    public class TreeDataGridExpanderCell : TreeDataGridCell
    {
        /// <summary>
        ///   Defines the <see cref="Indent" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridExpanderCell, int> IndentProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridExpanderCell, int>(
                nameof(Indent),
                o => o.Indent);

        /// <summary>
        ///   Defines the <see cref="IsExpanded" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridExpanderCell, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridExpanderCell, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded,
                (o, v) => o.IsExpanded = v);

        /// <summary>
        ///   Defines the <see cref="ShowExpander" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridExpanderCell, bool> ShowExpanderProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridExpanderCell, bool>(
                nameof(ShowExpander),
                o => o.ShowExpander);

        private Decorator? _contentContainer;
        private Type? _contentType;
        private TreeDataGridElementFactory? _factory;
        private int _indent;
        private bool _isExpanded;
        private IExpanderCell? _model;
        private bool _showExpander;

        /// <summary>
        ///   Gets the indentation level of the row that this cell belongs to.
        /// </summary>
        /// <value>
        ///   The number of indentation units, where each unit represents one level in the hierarchy.
        /// </value>
        public int Indent
        {
            get => _indent;
            private set => SetAndRaise(IndentProperty, ref _indent, value);
        }

        /// <summary>
        ///   Gets or sets a value indicating whether the row is expanded to show child rows.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_model is object) _model.IsExpanded = value; }
        }

        /// <summary>
        ///   Gets a value indicating whether the expander button should be visible.
        /// </summary>
        /// <value>
        ///   true if the expander button should be visible; otherwise, false.
        /// </value>
        /// <remarks>
        ///   When false, the cell still shows the proper indentation but without an expander button,
        ///   indicating a leaf node or a row that cannot be expanded.
        /// </remarks>
        public bool ShowExpander
        {
            get => _showExpander;
            private set => SetAndRaise(ShowExpanderProperty, ref _showExpander, value);
        }

        /// <summary>
        ///   Prepares the cell for display with the specified data.
        /// </summary>
        /// <param name="factory">The element factory used to create child elements.</param>
        /// <param name="selection">The selection interaction model.</param>
        /// <param name="model">The cell's data model.</param>
        /// <param name="columnIndex">The index of the cell's column.</param>
        /// <param name="rowIndex">The index of the cell's row.</param>
        public override void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection,
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            if (_model is object)
                throw new InvalidOperationException("Cell is already realized.");

            if (model is IExpanderCell expanderModel)
            {
                _factory = factory;
                _model = expanderModel;
                Indent = (_model.Row as IIndentedRow)?.Indent ?? 0;
                ShowExpander = _model.ShowExpander;

                // We can't go via the `IsExpanded` property here as that contains the implementation
                // for changing the expanded state by user action; it signals to the model that the
                // state is changed but here we need to update our state from the model.
                SetAndRaise(IsExpandedProperty, ref _isExpanded, _model.IsExpanded);

                if (expanderModel is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged += ModelPropertyChanged;
            }
            else
            {
                throw new InvalidOperationException("Invalid cell model.");
            }

            base.Realize(factory, selection, model, columnIndex, rowIndex);
            UpdateContent(_factory);
        }

        /// <summary>
        ///   Releases resources used by the cell and prepares it for reuse.
        /// </summary>
        /// <inheritdoc />
        public override void Unrealize()
        {
            if (_model is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= ModelPropertyChanged;
            _model = null;
            base.Unrealize();
            if (_factory is object)
                UpdateContent(_factory);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _contentContainer = e.NameScope.Find<Decorator>("PART_Content");
            if (_factory is object)
                UpdateContent(_factory);
        }

        private void UpdateContent(TreeDataGridElementFactory factory)
        {
            if (_contentContainer is null)
                return;

            if (_model?.Content is ICell innerModel)
            {
                var contentType = innerModel.GetType();

                if (contentType != _contentType)
                {
                    var element = factory.GetOrCreateElement(innerModel, this);
                    element.IsVisible = true;
                    _contentContainer.Child = element;
                    _contentType = contentType;
                }

                if (_contentContainer.Child is ITreeDataGridCell innerCell)
                    innerCell.Realize(factory, null, innerModel, ColumnIndex, RowIndex);
            }
            else if (_contentContainer.Child is ITreeDataGridCell innerCell)
            {
                innerCell.Unrealize();
            }
        }

        private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_model is null)
                return;

            if (e.PropertyName == nameof(_model.IsExpanded))
                SetAndRaise(IsExpandedProperty, ref _isExpanded, _model.IsExpanded);
            if (e.PropertyName == nameof(_model.ShowExpander))
                ShowExpander = _model.ShowExpander;
        }
    }
}
