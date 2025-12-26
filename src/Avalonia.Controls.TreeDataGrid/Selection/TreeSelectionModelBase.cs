using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Controls.Selection
{
    /// <summary>
    ///   Base class for selection models in TreeDataGrid.
    /// </summary>
    /// <typeparam name="T">The type of items being selected.</typeparam>
    /// <remarks>
    ///   <para>
    ///     TreeSelectionModelBase provides the core functionality for hierarchical selection,
    ///     supporting both single and multiple selection modes. It tracks selection using
    ///     <see cref="IndexPath" /> instances that represent positions in the hierarchical structure.
    ///   </para>
    ///   <para>
    ///     This class implements the <see cref="ITreeSelectionModel" /> interface and provides events
    ///     for tracking selection changes, property changes, and changes to the underlying data structure.
    ///   </para>
    ///   <para>
    ///     Derived classes must implement <see cref="TreeSelectionModelBase{T}.GetChildren(T)" /> to define how the hierarchical
    ///     structure is navigated.
    ///   </para>
    /// </remarks>
    public abstract class TreeSelectionModelBase<T> : ITreeSelectionModel, INotifyPropertyChanged
    {
        private readonly TreeSelectionNode<T> _root;
        private int _count;
        private bool _singleSelect = true;
        private IndexPath _anchorIndex;
        private IndexPath _rangeAnchorIndex;
        private IndexPath _selectedIndex;
        private Operation? _operation;
        private TreeSelectedIndexes<T>? _selectedIndexes;
        private TreeSelectedItems<T>? _selectedItems;
        private int _collectionChanging;
        private EventHandler<TreeSelectionModelSelectionChangedEventArgs>? _untypedSelectionChanged;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeSelectionModelBase{T}" /> class.
        /// </summary>
        protected TreeSelectionModelBase()
        {
            _root = new(this);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeSelectionModelBase{T}" /> class with a
        ///   specified data source.
        /// </summary>
        /// <param name="source">The hierarchical data source.</param>
        protected TreeSelectionModelBase(IEnumerable source)
            : this()
        {
            Source = source;
        }

        /// <inheritdoc />
        public int Count 
        {
            get => _count;
            private set
            {
                if (_count != value)
                {
                    _count = value;
                    RaisePropertyChanged(nameof(Count));
                }
            }
        }

        /// <inheritdoc />
        public bool SingleSelect 
        {
            get => _singleSelect;
            set
            {
                if (_singleSelect != value)
                {
                    if (value == true)
                    {
                        SelectedIndex = _selectedIndex;
                    }

                    _singleSelect = value;

                    RaisePropertyChanged(nameof(SingleSelect));
                }
            }
        }

        /// <inheritdoc />
        public IndexPath SelectedIndex 
        {
            get => _selectedIndex;
            set
            {
                using var update = BatchUpdate();
                Clear();
                Select(value, updateRangeAnchorIndex: true);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IndexPath> SelectedIndexes => _selectedIndexes ??= new(this);
        /// <summary>
        ///   Gets the currently selected item.
        /// </summary>
        /// <value>
        ///   The selected item, or default(T) if no item is selected.
        /// </value>
        /// <remarks>
        ///   When multiple items are selected, this property returns the first selected item.
        /// </remarks>
        public T? SelectedItem
        {
            get => Source is null || _selectedIndex == default ? default : GetSelectedItemAt(_selectedIndex);
        }

        /// <summary>
        ///   Gets a collection containing all selected items.
        /// </summary>
        /// <value>
        ///   A read-only list of the selected items.
        /// </value>
        public IReadOnlyList<T?> SelectedItems => _selectedItems ??= new(this);

        /// <inheritdoc />
        public IndexPath AnchorIndex 
        {
            get => _anchorIndex;
            set
            {
                if (!TryGetItemAt(value, out _))
                    return;
                using var update = BatchUpdate();
                update.Operation.AnchorIndex = value;
            }
        }

        /// <inheritdoc />
        public IndexPath RangeAnchorIndex
        {
            get => _rangeAnchorIndex;
            set
            {
                if (!TryGetItemAt(value, out _))
                    return;
                using var update = BatchUpdate();
                update.Operation.RangeAnchorIndex = value;
            }
        }

        object? ITreeSelectionModel.SelectedItem => SelectedItem;
        IReadOnlyList<object?> ITreeSelectionModel.SelectedItems => _selectedItems ??= new(this);

        IEnumerable? ITreeSelectionModel.Source
        {
            get => Source;
            set => throw new NotSupportedException();
        }

        internal TreeSelectionNode<T> Root => _root;

        /// <summary>
        ///   Gets a value indicating whether the source collection is currently being changed.
        /// </summary>
        protected bool IsSourceCollectionChanging => _collectionChanging > 0;

        /// <summary>
        ///   Gets or sets the data source for the selection model.
        /// </summary>
        /// <remarks>
        ///   When this property is set to a new value, any existing selection is cleared.
        /// </remarks>
        protected IEnumerable? Source
        {
            get => _root.Source;
            set
            {
                if (_root.Source != value)
                {
                    if (_root.Source is object && value is object)
                    {
                        using var update = BatchUpdate();
                        Clear();
                    }

                    _root.Source = value;
                }
            }
        }

        /// <summary>
        ///   Occurs when the selection changes.
        /// </summary>
        /// <remarks>
        ///   This event provides strongly-typed access to the selection changes, including the
        ///   items and index paths that were selected and deselected.
        /// </remarks>
        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs<T>>? SelectionChanged;
        /// <summary>
        ///   Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        ///   Occurs when item indexes change due to insertions or removals in the data source.
        /// </summary>
        public event EventHandler<TreeSelectionModelIndexesChangedEventArgs>? IndexesChanged;
        /// <summary>
        ///   Occurs when the data source is reset.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This event is raised when a collection in the hierarchical data structure is reset,
        ///     such as when a collection is cleared or replaced with a new collection.
        ///   </para>
        ///   <para>
        ///     Due to design limitations of collection reset events, the TreeSelectionModel cannot
        ///     automatically preserve selection state when a collection is reset. The reset event
        ///     doesn't provide information about which items were removed or how they map to new
        ///     items.
        ///   </para>
        ///   <para>
        ///     Applications that need to maintain selection across collection resets must handle
        ///     this event and manually restore the selection based on their knowledge of the data
        ///     model. For example, you might store the IDs or unique identifiers of selected items
        ///     before the reset, and then re-select them after the reset by finding the new indices
        ///     of those items.
        ///   </para>
        ///   <para>
        ///     The <see cref="TreeSelectionModelSourceResetEventArgs.ParentIndex" /> property indicates
        ///     which part of the hierarchical structure was reset, allowing you to determine whether
        ///     to restore selection for the entire collection or just a subtree.
        ///   </para>
        /// </remarks>
        public event EventHandler<TreeSelectionModelSourceResetEventArgs>? SourceReset;

        event EventHandler<TreeSelectionModelSelectionChangedEventArgs>? ITreeSelectionModel.SelectionChanged
        {
            add => _untypedSelectionChanged += value;
            remove => _untypedSelectionChanged -= value;
        }

        /// <summary>
        ///   Creates a batch update operation that will defer selection change notifications until
        ///   disposed.
        /// </summary>
        /// <returns>
        ///   A disposable object that, when disposed, will commit the selection changes and raise
        ///   appropriate events.
        /// </returns>
        /// <remarks>
        ///   Use this method with a using statement to group multiple selection operations together and
        ///   raise only a single set of change events when the batch is complete. This is equivalent to
        ///   calling <see cref="TreeSelectionModelBase{T}.BeginBatchUpdate()" /> and <see cref="TreeSelectionModelBase{T}.EndBatchUpdate()" />.
        /// </remarks>
        public BatchUpdateOperation BatchUpdate() => new(this);

        /// <inheritdoc />
        public void BeginBatchUpdate()
        {
            _operation ??= new Operation(this);
            ++_operation.UpdateCount;
        }

        /// <inheritdoc />
        public void EndBatchUpdate()
        {
            if (_operation is null || _operation.UpdateCount == 0)
                throw new InvalidOperationException("No batch update in progress.");
            if (--_operation.UpdateCount == 0)
                CommitOperation(_operation);
        }
        
        /// <inheritdoc />
        public void Clear()
        {
            using var update = BatchUpdate();
            var o = update.Operation;
            _root.Clear(o);
            o.SelectedIndex = default;
        }

        /// <inheritdoc />
        public void Deselect(IndexPath index)
        {
            if (!IsSelected(index))
                return;

            using var update = BatchUpdate();
            var o = update.Operation;

            o.DeselectedRanges ??= [];
            o.SelectedRanges?.Remove(index);
            o.DeselectedRanges.Add(index);

            if (o.DeselectedRanges?.Contains(_selectedIndex) == true)
                o.SelectedIndex = GetFirstSelectedIndex(_root, except: o.DeselectedRanges);
        }

        /// <inheritdoc />
        public bool IsSelected(IndexPath index)
        {
            if (index == default)
                return false;
            var node = GetNode(index[..^1]);
            return IndexRange.Contains(node?.Ranges, index[^1]);
        }

        /// <inheritdoc />
        public void Select(IndexPath index) => Select(index, updateRangeAnchorIndex: false);

        /// <summary>
        ///   Gets the children of a node in the hierarchical data structure.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <returns>
        ///   A collection containing the children of the specified node, or null if the node has no
        ///   children.
        /// </returns>
        /// <remarks>
        ///   This method must be implemented by derived classes to define how the hierarchical
        ///   structure is navigated.
        /// </remarks>
        protected internal abstract IEnumerable<T>? GetChildren(T node);
        
        /// <summary>
        ///   Attempts to get the item at the specified index path.
        /// </summary>
        /// <param name="index">The index path to the item.</param>
        /// <param name="result">
        ///   When this method returns, contains the item at the specified index path if found;
        ///   otherwise, the default value for type <typeparamref name="T" />.
        /// </param>
        /// <returns>
        ///   true if an item was found at the specified index path; otherwise, false.
        /// </returns>
        /// <remarks>
        ///   This method navigates the hierarchical data structure to find the item at the
        ///   specified index path.
        /// </remarks>
        protected virtual bool TryGetItemAt(IndexPath index, out T? result)
        {
            var items = (IEnumerable<T>?)_root.ItemsView;
            var count = index.Count;

            for (var i = 0; i < count; ++i)
            {
                if (items is null)
                {
                    result = default;
                    return false;
                }

                if (TryGetElementAt(items, index[i], out var item))
                {
                    if (i == count - 1)
                    {
                        result = item;
                        return true;
                    }
                    else
                    {
                        items = GetChildren(item);
                    }
                }
                else
                {
                    break;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        ///   Called when the source collection change operation is finished.
        /// </summary>
        /// <remarks>
        ///   Override this method to perform operations after the source collection has
        ///   completed changing.
        /// </remarks>
        protected virtual void OnSourceCollectionChangeFinished()
        {
        }

        /// <summary>
        ///   Raises the <see cref="TreeSelectionModelBase{T}.PropertyChanged" /> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal T GetSelectedItemAt(in IndexPath path)
        {
            if (path == default)
                throw new ArgumentOutOfRangeException(nameof(path));
            if (Source is null)
                throw new InvalidOperationException("Cannot get item from null Source.");

            if (path != default)
            {
                var node = GetNode(path[..^1]);

                if (node is not null)
                    return node.ItemsView![path[^1]];
            }

            throw new ArgumentOutOfRangeException(nameof(path));
        }

        internal void OnNodeCollectionChangeStarted()
        {
            ++_collectionChanging;
        }

        internal void OnNodeCollectionChanged(
            IndexPath parentIndex,
            int shiftStartIndex,
            int shiftEndIndex,
            int shiftDelta,
            bool raiseIndexesChanged,
            IReadOnlyList<T?>? removed)
        {
            if (_operation?.UpdateCount > 0)
                throw new InvalidOperationException("Source collection was modified during selection update.");
            if (shiftDelta == 0 && !(removed?.Count > 0))
                return;

            if (raiseIndexesChanged)
            {
                IndexesChanged?.Invoke(
                    this,
                    new TreeSelectionModelIndexesChangedEventArgs(parentIndex, shiftStartIndex, shiftEndIndex, shiftDelta));
            }

            // Shift or clear the selected and anchor indexes according to the shift index/delta.
            var hadSelection = _selectedIndex != default;
            var selectedIndexChanged = ShiftIndex(parentIndex, shiftStartIndex, shiftDelta, ref _selectedIndex);
            var anchorIndexChanged = ShiftIndex(parentIndex, shiftStartIndex, shiftDelta, ref _anchorIndex);
            var selectedItemChanged = false;

            // Check that the selected index is still selected in the node. It can get
            // unselected as the result of a replace operation.
            if (hadSelection && !IsSelected(_selectedIndex))
            {
                _selectedIndex = GetFirstSelectedIndex(_root);
                selectedIndexChanged = selectedItemChanged = true;
            }

            if (removed?.Count > 0 && (SelectionChanged is not null || _untypedSelectionChanged is not null))
            {
                var e = new TreeSelectionModelSelectionChangedEventArgs<T>(deselectedItems: removed);
                SelectionChanged?.Invoke(this, e);
                _untypedSelectionChanged?.Invoke(this, e);
            }

            if (removed?.Count > 0)
                Count -= removed.Count;

            if (selectedIndexChanged)
                RaisePropertyChanged(nameof(SelectedIndex));
            if (selectedItemChanged)
                RaisePropertyChanged(nameof(SelectedItem));
            if (anchorIndexChanged)
                RaisePropertyChanged(nameof(AnchorIndex));
        }

        internal void OnNodeCollectionChangeFinished()
        {
            if (--_collectionChanging == 0)
                OnSourceCollectionChangeFinished();
        }

        /// <summary>
        ///   Called when a node's collection is reset.
        /// </summary>
        /// <param name="parentIndex">The index path to the parent of the reset collection.</param>
        /// <param name="removeCount">The number of items removed by the reset operation.</param>
        /// <remarks>
        ///   This method is called when a collection in the hierarchical structure is reset,
        ///   such as when an entirely new collection is assigned.
        /// </remarks>
        protected internal virtual void OnNodeCollectionReset(IndexPath parentIndex, int removeCount)
        {
            var selectedIndexChanged = false;
            var anchorIndexChanged = false;
            var selectedItemChanged = false;

            // Check that the selected index is still selected in the node. It can get
            // unselected as the result of a replace operation.
            if (_selectedIndex != default && !IsSelected(_selectedIndex))
            {
                _selectedIndex = GetFirstSelectedIndex(_root);
                selectedIndexChanged = selectedItemChanged = true;
            }

            // If the anchor index is invalid, clear it.
            if (_anchorIndex != default && !TryGetItemAt(_anchorIndex, out _))
            {
                _anchorIndex = default;
                anchorIndexChanged = true;
            }

            Count -= removeCount;
            SourceReset?.Invoke(this, new TreeSelectionModelSourceResetEventArgs(parentIndex));

            if (selectedIndexChanged)
                RaisePropertyChanged(nameof(SelectedIndex));
            if (selectedItemChanged)
                RaisePropertyChanged(nameof(SelectedItem));
            if (anchorIndexChanged)
                RaisePropertyChanged(nameof(AnchorIndex));
        }

        private IndexPath GetFirstSelectedIndex(TreeSelectionNode<T> node, IndexRanges? except = null)
        {
            if (node.Ranges.Count > 0)
            {
                var count = IndexRange.GetCount(node.Ranges);
                var index = 0;

                while (index < count)
                {
                    var result = node.Path.Append(IndexRange.GetAt(node.Ranges, index++));
                    if (except?.Contains(result) != true)
                        return result;
                }
            }
            
            if (node.Children is object)
            {
                foreach (var child in node.Children)
                {
                    if (child is not null)
                    {
                        var i = GetFirstSelectedIndex(child, except);
                        
                        if (i != default)
                            return i;
                    }
                }
            }

            return default;
        }

        private TreeSelectionNode<T>? GetNode(in IndexPath path)
        {
            var depth = path.Count;
            var node = _root;

            for (var i = 0; i < depth; ++i)
            {
                node = node!.GetChild(path[i]);
                if (node is null)
                    break;
            }

            return node;
        }

        private TreeSelectionNode<T>? GetOrCreateNode(in IndexPath path)
        {
            var depth = path.Count;
            var node = _root;

            for (var i = 0; i < depth; ++i)
            {
                node = node!.GetOrCreateChild(path[i]);
                if (node is null)
                    break;
            }

            return node;
        }

        private void Select(IndexPath index, bool updateRangeAnchorIndex)
        {
            if (index == default || !TryGetItemAt(index, out _))
                return;

            using var update = BatchUpdate();
            var o = update.Operation;

            if (SingleSelect)
                Clear();

            o.DeselectedRanges?.Remove(index);

            if (!IsSelected(index))
            {
                o.SelectedRanges ??= [];
                o.SelectedRanges.Add(index);
            }

            if (o.SelectedIndex == default)
                o.SelectedIndex = index;

            o.AnchorIndex = index;

            if (updateRangeAnchorIndex)
                o.RangeAnchorIndex = index;
        }

        private void CommitOperation(Operation operation)
        {
            var oldAnchorIndex = _anchorIndex;
            var oldRangeAnchorIndex = _rangeAnchorIndex;
            var oldSelectedIndex = _selectedIndex;
            var indexesChanged = false;

            _selectedIndex = operation.SelectedIndex;
            _anchorIndex = operation.AnchorIndex;
            _rangeAnchorIndex = operation.RangeAnchorIndex;

            if (operation.SelectedRanges is not null)
            {
                indexesChanged |= CommitSelect(operation.SelectedRanges) > 0;
            }

            if (operation.DeselectedRanges is not null)
            {
                indexesChanged |= CommitDeselect(operation.DeselectedRanges) > 0;
            }

            Count += (operation.SelectedRanges?.Count ?? 0) - (operation.DeselectedRanges?.Count ?? 0);

            if ((SelectionChanged is not null || _untypedSelectionChanged is not null) &&
                (operation.DeselectedRanges?.Count > 0 ||
                 operation.SelectedRanges?.Count > 0 ||
                 operation.DeselectedItems is object))
            {
                var deselectedIndexes = operation.DeselectedRanges;
                var selectedIndexes = operation.SelectedRanges;
                var deselectedItems = operation.DeselectedItems ??
                    TreeSelectionChangedItems<T>.Create(this, deselectedIndexes);

                var e = new TreeSelectionModelSelectionChangedEventArgs<T>(
                    deselectedIndexes,
                    selectedIndexes,
                    deselectedItems,
                    TreeSelectionChangedItems<T>.Create(this, selectedIndexes));
                SelectionChanged?.Invoke(this, e);
                _untypedSelectionChanged?.Invoke(this, e);
            }

            _root.PruneEmptyChildren();

            if (oldSelectedIndex != _selectedIndex)
            {
                indexesChanged = true;
                RaisePropertyChanged(nameof(SelectedIndex));
                RaisePropertyChanged(nameof(SelectedItem));
            }

            if (oldAnchorIndex != _anchorIndex)
                RaisePropertyChanged(nameof(AnchorIndex));

            if (oldRangeAnchorIndex != _rangeAnchorIndex)
                RaisePropertyChanged(nameof(RangeAnchorIndex));

            if (indexesChanged)
            {
                RaisePropertyChanged(nameof(SelectedIndexes));
                RaisePropertyChanged(nameof(SelectedItems));
            }

            _operation = null;
        }

        private int CommitSelect(IndexRanges selectedRanges)
        {
            var result = 0;

            foreach (var row in selectedRanges.Ranges)
            {
                var parent = row.Key;
                var ranges = row.Value;
                var node = GetOrCreateNode(parent);

                if (node is not null)
                {
                    foreach (var range in ranges)
                        result += node.CommitSelect(range);
                }
            }

            return result;
        }

        private int CommitDeselect(IndexRanges selectedRanges)
        {
            var result = 0;

            foreach (var row in selectedRanges.Ranges)
            {
                var parent = row.Key;
                var ranges = row.Value;
                var node = GetOrCreateNode(parent);

                if (node is not null)
                {
                    foreach (var range in ranges)
                        result += node.CommitDeselect(range);
                }
            }

            return result;
        }

        internal static bool ShiftIndex(IndexPath parentIndex, int shiftIndex, int shiftDelta, ref IndexPath path)
        {
            if (parentIndex.IsAncestorOf(path) && path[parentIndex.Count] >= shiftIndex)
            {
                var changeDepth = parentIndex.Count;
                var pathIndex = path[changeDepth];

                if (shiftDelta < 0 && pathIndex >= shiftIndex && pathIndex < shiftIndex - shiftDelta)
                {
                    // Item was removed, clear the path.
                    path = default;
                    return true;
                }

                if (pathIndex >= shiftIndex)
                {
                    // Item remains, but index was shifted.
                    var indexes = path.ToArray();
                    indexes[changeDepth] += shiftDelta;
                    path = new IndexPath(indexes);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetElementAt(IEnumerable<T> items, int index, [MaybeNullWhen(false)] out T result)
        { 
            if (items is IList<T> list)
            {
                if (index < list.Count)
                {
                    result = list[index];
                    return true;
                }
            }
            else if (items is IReadOnlyList<T> ro)
            {
                if (index < ro.Count)
                {
                    result = ro[index];
                    return true;
                }
            }
            else
            {
                foreach (var item in items)
                {
                    if (index-- == 0)
                    {
                        result = item;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        ///   Represents a batch update operation that defers selection change notifications.
        /// </summary>
        /// <remarks>
        ///   This struct implements <see cref="IDisposable" /> to allow using it with a using statement
        ///   to automatically end the batch update when the using block exits.
        /// </remarks>
        public record struct BatchUpdateOperation : IDisposable
        {
            private readonly TreeSelectionModelBase<T> _owner;
            private bool _isDisposed;

            /// <summary>
            ///   Initializes a new instance of the <see cref="TreeSelectionModelBase{T}.BatchUpdateOperation" /> struct.
            /// </summary>
            /// <param name="owner">The selection model that owns this batch update.</param>
            public BatchUpdateOperation(TreeSelectionModelBase<T> owner)
            {
                _owner = owner;
                _isDisposed = false;
                owner.BeginBatchUpdate();
            }

            internal readonly Operation Operation => _owner._operation!;

            /// <summary>
            ///   Ends the batch update operation and commits the selection changes.
            /// </summary>
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _owner?.EndBatchUpdate();
                    _isDisposed = true;
                }
            }
        }

        internal class Operation
        {
            public Operation(TreeSelectionModelBase<T> owner)
            {
                AnchorIndex = owner.AnchorIndex;
                RangeAnchorIndex = owner.RangeAnchorIndex;
                SelectedIndex = owner.SelectedIndex;
            }

            public int UpdateCount { get; set; }
            public bool IsSourceUpdate { get; set; }
            public IndexPath AnchorIndex { get; set; }
            public IndexPath RangeAnchorIndex { get; set; }
            public IndexPath SelectedIndex { get; set; }
            public IndexRanges? SelectedRanges { get; set; }
            public IndexRanges? DeselectedRanges { get; set; }
            public IReadOnlyList<T?>? DeselectedItems { get; set; }
        }
    }
}
