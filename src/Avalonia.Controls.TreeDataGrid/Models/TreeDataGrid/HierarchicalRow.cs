using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Avalonia.Controls.Models.TreeDataGrid
{
    /// <summary>
    ///   A row in a <see cref="HierarchicalTreeDataGridSource{TModel}" /> that represents an item in
    ///   a hierarchical data structure.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <remarks>
    ///   <para>
    ///     The <see cref="HierarchicalRow{TModel}" /> class represents a row in a hierarchical data
    ///     structure within a  TreeDataGrid. It manages the expanded/collapsed state of a row, its
    ///     position within the hierarchy, and its relationship to its child rows.
    ///   </para>
    ///   <para>
    ///     This class coordinates with an <see cref="IExpanderColumn{TModel}" /> to handle the tree
    ///     structure visualization, and an <see cref="IExpanderRowController{TModel}" /> to communicate
    ///     row state changes back to the data source.
    ///   </para>
    ///   <para>
    ///     Each row maintains its position within the tree using an <see cref="IndexPath" />, which
    ///     represents the path from the root to this row in the hierarchy. This path is used for
    ///     selection, sorting, and navigation operations.
    ///   </para>
    /// </remarks>
    public class HierarchicalRow<TModel> : NotifyingBase,
        IExpanderRow<TModel>,
        IIndentedRow,
        IModelIndexableRow,
        IDisposable
    {
        private readonly IExpanderRowController<TModel> _controller;
        private readonly IExpanderColumn<TModel> _expanderColumn;
        private Comparison<TModel>? _comparison;
        private IEnumerable<TModel>? _childModels;
        private ChildRows? _childRows;
        private bool _isExpanded;
        private bool? _showExpander;

#if !NET5_0_OR_GREATER
        object? IRow.Model => Model;
#endif

        /// <summary>
        ///   Initializes a new instance of the <see cref="HierarchicalRow{TModel}" /> class,
        ///   representing a row in a hierarchical data structure.
        /// </summary>
        /// <param name="controller">
        ///   The controller responsible for managing the behavior of the row within the hierarchy.
        /// </param>
        /// <param name="expanderColumn">
        ///   The column that provides expansion functionality for the row.
        /// </param>
        /// <param name="modelIndex">
        ///   The index path representing the position of the row within the hierarchy. Must contain
        ///   at least one element.
        /// </param>
        /// <param name="model">The data model associated with the row.</param>
        /// <param name="comparison">
        ///   An optional comparison delegate used to determine the sorting of child rows.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   Thrown if <paramref name="modelIndex" /> is empty.
        /// </exception>
        public HierarchicalRow(
            IExpanderRowController<TModel> controller,
            IExpanderColumn<TModel> expanderColumn,
            IndexPath modelIndex,
            TModel model,
            Comparison<TModel>? comparison)
        {
            if (modelIndex.Count == 0)
                throw new ArgumentException("Invalid model index");

            _controller = controller;
            _expanderColumn = expanderColumn;
            _comparison = comparison;
            ModelIndexPath = modelIndex;
            Model = model;
        }

        /// <summary>
        ///   Gets the row's visible child rows.
        /// </summary>
        /// <remarks>
        ///   Returns the collection of child rows when the row is expanded, or null when the row
        ///   is collapsed. Child rows are created on-demand when the row is first expanded.
        /// </remarks>
        public IReadOnlyList<HierarchicalRow<TModel>>? Children => _isExpanded ? _childRows : null;

        /// <summary>
        ///   Gets the index of the model relative to its parent.
        /// </summary>
        /// <remarks>
        ///   To retrieve the index path to the model from the root data source, see
        ///   <see cref="HierarchicalRow{TModel}.ModelIndexPath" />.
        /// </remarks>
        public int ModelIndex => ModelIndexPath[^1];

        /// <summary>
        ///   Gets the index path of the model in the data source.
        /// </summary>
        /// <remarks>
        ///   This index path uniquely identifies the row within the hierarchical structure, starting
        ///   from the root. It represents the sequence of child indices needed to navigate to this
        ///   row.
        /// </remarks>
        public IndexPath ModelIndexPath { get; private set; }

        /// <summary>
        ///   Gets the row header.
        /// </summary>
        /// <remarks>
        ///   Returns the row's <see cref="HierarchicalRow{TModel}.ModelIndexPath" />, which can be used to identify the row
        ///   within the hierarchy.
        /// </remarks>
        public object? Header => ModelIndexPath;
        /// <summary>
        ///   Gets the indentation level of the row.
        /// </summary>
        /// <remarks>
        ///   The indentation level is determined by the depth of the row in the hierarchical
        ///   structure. Root items have an indent of 0, their children have an indent of 1, and so
        ///   on.
        /// </remarks>
        public int Indent => ModelIndexPath.Count - 1;
        /// <summary>
        ///   Gets the model object associated with this row.
        /// </summary>
        public TModel Model { get; }

        /// <summary>
        ///   Gets or sets the height of the row.
        /// </summary>
        /// <remarks>
        ///   Currently, row height is always auto-sized and setting this property has no effect.
        /// </remarks>
        public GridLength Height 
        {
            get => GridLength.Auto;
            set { }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether the row is expanded to show its child rows.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     When set to true, the row will attempt to expand and show its child rows.
        ///     If the row has no children, the expander will be hidden.
        ///   </para>
        ///   <para>
        ///     Setting this property will notify the controller of the state change, which
        ///     will update the visual representation in the UI.
        ///   </para>
        /// </remarks>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    if (value)
                        Expand();
                    else
                        Collapse();
                }
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the expander button should be shown for this row.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The expander button is shown when the row has children that can be expanded. This
        ///     property's initial value is determined by querying the expander column's
        ///     <see cref="IExpanderColumn{TModel}.HasChildren(TModel)" /> method.
        ///   </para>
        ///   <para>
        ///     If a row is expanded but turns out to have no children, the expander will be hidden.
        ///   </para>
        /// </remarks>
        public bool ShowExpander
        {
            get => _showExpander ??= _expanderColumn.HasChildren(Model);
            private set => RaiseAndSetIfChanged(ref _showExpander, value);
        }

        /// <summary>
        ///   Disposes resources used by this row, including any child rows.
        /// </summary>
        public void Dispose() => _childRows?.Dispose();

        /// <summary>
        ///   Updates the model index of this row by adding a delta to the last component of the
        ///   model index path.
        /// </summary>
        /// <param name="delta">The amount to add to the model index.</param>
        /// <remarks>
        ///   This method is used when items are inserted or removed from the data source, causing
        ///   the indices of subsequent items to shift. After updating this row's index, it
        ///   recursively updates the indices of all child rows.
        /// </remarks>
        public void UpdateModelIndex(int delta)
        {
            ModelIndexPath = ModelIndexPath[..^1].Append(ModelIndexPath[^1] + delta);

            if (_childRows is null)
                return;

            var childCount = _childRows.Count;

            for (var i = 0; i < childCount; ++i)
                _childRows[i].UpdateParentModelIndex(ModelIndexPath);
        }

        /// <summary>
        ///   Updates this row's model index path when its parent's index path has changed.
        /// </summary>
        /// <param name="parentIndex">The new parent index path.</param>
        /// <remarks>
        ///   This method is called when a parent row's index path changes, requiring all child rows
        ///   to update their paths accordingly. After updating this row's path, it recursively
        ///   updates the paths of all child rows.
        /// </remarks>
        public void UpdateParentModelIndex(IndexPath parentIndex)
        {
            ModelIndexPath = parentIndex.Append(ModelIndex);

            if (_childRows is null)
                return;

            var childCount = _childRows.Count;

            for (var i = 0; i < childCount; ++i)
                _childRows[i].UpdateParentModelIndex(ModelIndexPath);
        }

        void IExpanderRow<TModel>.UpdateShowExpander(IExpanderCell cell, bool value)
        {
            ShowExpander = value;
        }

        internal void SortChildren(Comparison<TModel>? comparison)
        {
            _comparison = comparison;

            if (_childRows is null)
                return;

            _childRows.Sort(comparison);

            foreach (var row in _childRows)
            {
                row.SortChildren(comparison);
            }
        }

        private void Expand()
        {
            if (!_expanderColumn.HasChildren(Model))
            {
                _expanderColumn.SetModelIsExpanded(this);
                return;
            }

            _controller.OnBeginExpandCollapse(this);

            var oldExpanded = _isExpanded;
            var childModels = _expanderColumn.GetChildModels(Model);

            if (_childModels != childModels)
            {
                _childModels = childModels;
                _childRows?.Dispose();
                _childRows = new ChildRows(
                    this,
                    TreeDataGridItemsSourceView<TModel>.GetOrCreate(childModels),
                    _comparison);
            }

            if (_childRows?.Count > 0)
                _isExpanded = true;
            else
                ShowExpander = false;

            _controller.OnChildCollectionChanged(this, CollectionExtensions.ResetEvent);

            if (_isExpanded != oldExpanded)
                RaisePropertyChanged(nameof(IsExpanded));

            _controller.OnEndExpandCollapse(this);
            _expanderColumn.SetModelIsExpanded(this);
        }

        private void Collapse()
        {
            _controller.OnBeginExpandCollapse(this);
            _isExpanded = false;
            _controller.OnChildCollectionChanged(this, CollectionExtensions.ResetEvent);
            RaisePropertyChanged(nameof(IsExpanded));
            _controller.OnEndExpandCollapse(this);
            _expanderColumn.SetModelIsExpanded(this);
        }

        private class ChildRows : SortableRowsBase<TModel, HierarchicalRow<TModel>>,
            IReadOnlyList<HierarchicalRow<TModel>>
        {
            private readonly HierarchicalRow<TModel> _owner;

            public ChildRows(
                HierarchicalRow<TModel> owner,
                TreeDataGridItemsSourceView<TModel> items,
                Comparison<TModel>? comparison)
                : base(items, comparison)
            {
                _owner = owner;
                CollectionChanged += OnCollectionChanged;
            }

            protected override HierarchicalRow<TModel> CreateRow(int modelIndex, TModel model)
            {
                return new HierarchicalRow<TModel>(
                    _owner._controller,
                    _owner._expanderColumn,
                    _owner.ModelIndexPath.Append(modelIndex),
                    model,
                    _owner._comparison);
            }

            private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (_owner.IsExpanded)
                    _owner._controller.OnChildCollectionChanged(_owner, e);
            }
        }
    }
}
