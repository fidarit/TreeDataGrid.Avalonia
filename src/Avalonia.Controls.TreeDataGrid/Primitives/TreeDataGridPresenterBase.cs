using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Rendering;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using CollectionExtensions = Avalonia.Controls.Models.TreeDataGrid.CollectionExtensions;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Base class for virtualized presenters used in <see cref="TreeDataGrid" /> to display rows
    ///   and columns.
    /// </summary>
    /// <typeparam name="TItem">The type of items to present.</typeparam>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridPresenterBase implements virtualization, for TreeDataGrid's column and row
    ///     presenters.
    ///   </para>
    ///   <para>
    ///     This class handles:
    ///     <list type="bullet">
    ///       <item>
    ///         <description>
    ///           Element creation and recycling using a <see cref="TreeDataGridElementFactory" />
    ///         </description>
    ///       </item>
    ///       <item>
    ///         <description>Viewport tracking and efficient scrolling</description>
    ///       </item>
    ///       <item>
    ///         <description>Item change notifications</description>
    ///       </item>
    ///       <item>
    ///         <description>Focused element tracking</description>
    ///       </item>
    ///       <item>
    ///         <description>Bringing items into view</description>
    ///       </item>
    ///     </list>
    ///   </para>
    ///   <para>
    ///     Derived classes must implement abstract methods to handle item-specific realization,
    ///     un-realization, and element index updates. They must also specify the orientation
    ///     in which items are laid out.
    ///   </para>
    /// </remarks>
    public abstract class TreeDataGridPresenterBase<TItem> : Border
    {
#pragma warning disable AVP1002
        /// <summary>
        ///   Defines the <see cref="TreeDataGridPresenterBase{TItem}.ElementFactory" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridPresenterBase<TItem>, TreeDataGridElementFactory?>
            ElementFactoryProperty =
                AvaloniaProperty.RegisterDirect<TreeDataGridPresenterBase<TItem>, TreeDataGridElementFactory?>(
                    nameof(ElementFactory),
                    o => o.ElementFactory,
                    (o, v) => o.ElementFactory = v);

        /// <summary>
        ///   Defines the <see cref="TreeDataGridPresenterBase{TItem}.Items" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridPresenterBase<TItem>, IReadOnlyList<TItem>?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridPresenterBase<TItem>, IReadOnlyList<TItem>?>(
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);
#pragma warning restore AVP1002
        private static readonly Rect s_invalidViewport = new(double.PositiveInfinity, double.PositiveInfinity, 0, 0);
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private TreeDataGridElementFactory? _elementFactory;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private IReadOnlyList<TItem>? _items;
        private bool _isSubscribedToItemChanges;
        private RealizedStackElements? _measureElements;
        private RealizedStackElements? _realizedElements;
        private ScrollViewer? _scrollViewer;
        private double _lastEstimatedElementSizeU = 25;
        private Control? _focusedElement;
        private int _focusedIndex = -1;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TreeDataGridPresenterBase{TItem}" /> class.
        /// </summary>
        public TreeDataGridPresenterBase()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;
        }

        /// <summary>
        ///   Gets or sets the factory used to create and recycle UI elements.
        /// </summary>
        /// <value>
        ///   The element factory used by this presenter.
        /// </value>
        /// <remarks>
        ///   The element factory is responsible for creating visual elements to represent data items,
        ///   and for recycling those elements when they are no longer needed.
        /// </remarks>
        public TreeDataGridElementFactory? ElementFactory
        {
            get => _elementFactory;
            set => SetAndRaise(ElementFactoryProperty, ref _elementFactory, value);
        }

        /// <summary>
        ///   Gets or sets the items to be presented.
        /// </summary>
        /// <value>
        ///   A read-only list of items.
        /// </value>
        /// <remarks>
        ///   When this property is set, the presenter will update its visual representation
        ///   and start listening for changes in the collection if it implements
        ///   <see cref="INotifyCollectionChanged" />.
        /// </remarks>
        public IReadOnlyList<TItem>? Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    UnsubscribeFromItemChanges();

                    var oldValue = _items;
                    _items = value;

                    SubscribeToItemChanges();

                    RaisePropertyChanged(
                        ItemsProperty,
                        oldValue,
                        _items);
                    OnItemsCollectionChanged(null, CollectionExtensions.ResetEvent);
                }
            }
        }

        internal IReadOnlyList<Control?> RealizedElements => _realizedElements?.Elements ?? [];

        /// <summary>
        ///   Gets the orientation in which items are arranged.
        /// </summary>
        /// <value>
        ///   The orientation (horizontal or vertical) in which items are arranged.
        /// </value>
        protected abstract Orientation Orientation { get; }
        protected Rect Viewport { get; private set; } = s_invalidViewport;

        /// <summary>
        ///   Brings an item at the specified index into the viewport.
        /// </summary>
        /// <param name="index">The index of the item to bring into view.</param>
        /// <param name="rect">An optional rectangle within the item to bring into view.</param>
        /// <returns>
        ///   The element representing the item if it was successfully brought into view; otherwise, null.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method attempts to scroll the presenter so that the item at the specified index
        ///     is visible within the viewport. If the item is already visible, it returns the existing
        ///     element. If the item is not visible, it creates a new element, measures it, and scrolls
        ///     it into view.
        ///   </para>
        ///   <para>
        ///     If a rect is specified, the presenter will attempt to bring that specific portion of
        ///     the item into view.
        ///   </para>
        /// </remarks>
        public Control? BringIntoView(int index, Rect? rect = null)
        {
            var items = Items;

            if (_isInLayout || items is null || index < 0 || index >= items.Count || _realizedElements is null)
                return null;

            if (GetRealizedElement(index) is Control element)
            {
                if (rect.HasValue)
                    element.BringIntoView(rect.Value);
                else
                    element.BringIntoView();
                return element;
            }
            else if (this.IsAttachedToVisualTree())
            {
                // Create and measure the element to be brought into view. Store it in a field so that
                // it can be re-used in the layout pass.
                var scrollToElement = GetOrCreateElement(items, index);
                scrollToElement.Measure(Size.Infinity);

                // Get the expected position of the element and put it in place.
                var anchorU = _realizedElements.GetOrEstimateElementU(index, ref _lastEstimatedElementSizeU);
                var elementRect = Orientation == Orientation.Horizontal ?
                    new Rect(anchorU, 0, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height) :
                    new Rect(0, anchorU, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height);
                scrollToElement.Arrange(elementRect);

                // Store the element and index so that they can be used in the layout pass.
                _scrollToElement = scrollToElement;
                _scrollToIndex = index;

                // If the item being brought into view was added since the last layout pass then
                // our bounds won't be updated, so any containing scroll viewers will not have an
                // updated extent. Do a layout pass to ensure that the containing scroll viewers
                // will be able to scroll the new item into view.
                if (!Bounds.Contains(elementRect) && !Viewport.Contains(elementRect))
                {
                    _isWaitingForViewportUpdate = true;
                    UpdateLayout();
                    _isWaitingForViewportUpdate = false;
                }

                // Try to bring the item into view and do a layout pass.
                if (rect.HasValue)
                    scrollToElement.BringIntoView(rect.Value);
                else
                    scrollToElement.BringIntoView();

                // If the viewport does not contain the item to scroll to, set _isWaitingForViewportUpdate:
                // this should cause the following chain of events:
                // - Measure is first done with the old viewport (which will be a no-op, see MeasureOverride)
                // - The viewport is then updated by the layout system which invalidates our measure
                // - Measure is then done with the new viewport.
                _isWaitingForViewportUpdate = !Viewport.Contains(elementRect);
                UpdateLayout();

                // If for some reason the layout system didn't give us a new viewport during the layout, we
                // need to do another layout pass as the one that took place was a no-op.
                if (_isWaitingForViewportUpdate)
                {
                    _isWaitingForViewportUpdate = false;
                    InvalidateMeasure();
                    UpdateLayout();
                }

                _scrollToElement = null;
                _scrollToIndex = -1;
                return scrollToElement;
            }

            return null;
        }

        /// <summary>
        ///   Gets all currently realized elements.
        /// </summary>
        public IEnumerable<Control> GetRealizedElements()
        {
            if (_realizedElements is not null)
                return _realizedElements.Elements.Where(x => x is not null)!;
            else
                return [];
        }

        /// <summary>
        ///   Attempts to get the element at the specified index.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <remarks>
        ///   This method returns the element only if it is currently realized. If the item
        ///   at the specified index is not currently visible, this method returns null.
        /// </remarks>
        public Control? TryGetElement(int index) => GetRealizedElement(index);

        internal void RecycleAllElements() => _realizedElements?.RecycleAllElements(_recycleElement);

        internal void RecycleAllElementsOnItemRemoved()
        {
            if (_realizedElements?.Count > 0)
            {
                _realizedElements?.ItemsRemoved(
                    _realizedElements.FirstIndex,
                    _realizedElements.Count,
                    _updateElementIndex,
                    _recycleElementOnItemRemoved);
            }
        }

        /// <summary>
        ///   Arranges a child element and returns the arranged bounds.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="element">The element to arrange.</param>
        /// <param name="rect">The rectangle within which the element should be arranged.</param>
        /// <returns>
        ///   The final arranged bounds of the element.
        /// </returns>
        /// <remarks>
        ///   This method is called during the arrange pass to position and size each child element.
        /// </remarks>
        protected virtual Rect ArrangeElement(int index, Control element, Rect rect)
        {
            element.Arrange(rect);
            return rect;
        }

        /// <summary>
        ///   Measures a child element and returns the desired size.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="element">The element to measure.</param>
        /// <param name="availableSize">The available size for the element.</param>
        /// <returns>
        ///   The desired size of the element.
        /// </returns>
        /// <remarks>
        ///   This method is called during the measure pass to determine the desired size of each
        ///   child element.
        /// </remarks>
        protected virtual Size MeasureElement(int index, Control element, Size availableSize)
        {
            element.Measure(availableSize);
            return element.DesiredSize;
        }

        /// <summary>
        ///   Gets the initial constraint for the first pass of the two-pass measure.
        /// </summary>
        /// <param name="element">The element being measured.</param>
        /// <param name="index">The index of the element.</param>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The measure constraint for the element.</returns>
        /// <remarks>
        ///   <para>
        ///     The measure pass is split into two parts:
        ///   </para>
        ///   <para>
        ///     - The initial pass is used to determine the "natural" size of the elements. In this
        ///     pass, infinity can be used as the measure constraint if the element has no other
        ///     constraints on its size.
        ///   </para>
        ///   <para>
        ///     - The final pass is made once the "natural" sizes of the elements are known and any
        ///     layout logic has been run. This pass is needed because controls should not be
        ///     arranged with a size less than that passed as the constraint during the measure
        ///     pass. This pass is only run if <see cref="TreeDataGridPresenterBase{TItem}.NeedsFinalMeasurePass(int, IReadOnlyList{Control?})" /> returns
        ///     true.
        ///   </para>
        /// </remarks>
        protected virtual Size GetInitialConstraint(
            Control element,
            int index,
            Size availableSize)
        {
            return availableSize;
        }

        /// <summary>
        ///   Called when the initial pass of the two-pass measure has been completed, in order to
        ///   determine whether a final measure pass is necessary.
        /// </summary>
        /// <param name="firstIndex">
        ///   The index of the first element in <paramref name="elements" />.
        /// </param>
        /// <param name="elements">The elements being measured.</param>
        /// <seealso cref="TreeDataGridPresenterBase{TItem}.GetInitialConstraint(Control, int, Size)" />
        protected virtual bool NeedsFinalMeasurePass(
            int firstIndex,
            IReadOnlyList<Control?> elements) => false;

        /// <summary>
        ///   Gets the final constraint for the second pass of the two-pass measure.
        /// </summary>
        /// <param name="element">The element being measured.</param>
        /// <param name="index">The index of the element.</param>
        /// <param name="availableSize">The available size.</param>
        /// <returns>
        ///   The measure constraint for the element.
        /// </returns>
        /// <seealso cref="TreeDataGridPresenterBase{TItem}.GetInitialConstraint(Control, int, Size)" />
        protected virtual Size GetFinalConstraint(
            Control element,
            int index,
            Size availableSize)
        {
            return element.DesiredSize;
        }

        /// <summary>
        ///   Creates a new element for the specified item.
        /// </summary>
        /// <param name="item">The item for which to create an element.</param>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        ///   A new control element for the item.
        /// </returns>
        /// <remarks>
        ///   This method is called when a new element needs to be created for an item.
        /// </remarks>
        protected virtual Control GetElementFromFactory(TItem item, int index)
        {
            return GetElementFromFactory(item!, index, this);
        }

        /// <summary>
        ///   Creates a new element for the specified data using the element factory.
        /// </summary>
        /// <param name="data">The data for which to create an element.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="parent">The parent control that will host the element.</param>
        /// <returns>
        ///   A new or recycled control element for the data.
        /// </returns>
        /// <remarks>
        ///   This method delegates to the element factory to create or recycle an element.
        /// </remarks>
        protected Control GetElementFromFactory(object data, int index, Control parent)
        {
            return _elementFactory!.GetOrCreateElement(data, parent);
        }

        /// <summary>
        ///   Gets the index and position of the element at the specified position.
        /// </summary>
        /// <param name="position">The position at which to find an element.</param>
        /// <returns>
        ///   A tuple containing:
        ///   - The index of the element, or -1 if no element was found at the position
        ///   - The position of the element
        /// </returns>
        /// <remarks>
        ///   This method is used to determine which element is at a specific position,
        ///   for example when handling pointer events.
        /// </remarks>
        protected virtual (int index, double position) GetElementAt(double position) => (-1, -1);
        /// <summary>
        ///   Gets the position of the element with the specified index.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>
        ///   The position of the element, or -1 if the element is not realized.
        /// </returns>
        /// <remarks>
        ///   This method is used to determine the position of an element with a specific index.
        /// </remarks>
        protected virtual double GetElementPosition(int index) => -1;
        /// <summary>
        ///   Prepares an element for display with the specified item data.
        /// </summary>
        /// <param name="element">The element to prepare.</param>
        /// <param name="item">The item data.</param>
        /// <param name="index">The index of the item.</param>
        /// <remarks>
        ///   This method must be implemented by derived classes to initialize the element
        ///   with the item data. It is called when an element is created or recycled.
        /// </remarks>
        protected abstract void RealizeElement(Control element, TItem item, int index);
        /// <summary>
        ///   Updates the index of an element when its position in the collection changes.
        /// </summary>
        /// <param name="element">The element to update.</param>
        /// <param name="oldIndex">The old index of the element.</param>
        /// <param name="newIndex">The new index of the element.</param>
        /// <remarks>
        ///   This method must be implemented by derived classes to update the element's
        ///   index when items are inserted or removed. It is called when collection changes
        ///   affect the indices of items.
        /// </remarks>
        protected abstract void UpdateElementIndex(Control element, int oldIndex, int newIndex);
        /// <summary>
        ///   Releases resources used by an element and prepares it for recycling.
        /// </summary>
        /// <param name="element">The element to unrealize.</param>
        /// <remarks>
        ///   This method must be implemented by derived classes to clean up an element
        ///   before it is recycled. It is called when an element is no longer needed for display.
        /// </remarks>
        protected abstract void UnrealizeElement(Control element);

        /// <summary>
        ///   Calculates the total size of all items in the primary axis.
        /// </summary>
        /// <param name="availableSize">The available size for measurement.</param>
        /// <returns>
        ///   The estimated total size of all items.
        /// </returns>
        /// <remarks>
        ///   This method calculates the estimated total size of all items based on the
        ///   average size of realized elements.
        /// </remarks>
        protected virtual double CalculateSizeU(Size availableSize)
        {
            if (Items is null)
                return 0;

            // Return the estimated size of all items based on the elements currently realized.
            return EstimateElementSizeU() * Items.Count;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;

            if (items is null || items.Count == 0)
            {
                TrimUnrealizedChildren();
                return default;
            }

            var orientation = Orientation;

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
                return EstimateDesiredSize(orientation, items.Count);

            _isInLayout = true;

            try
            {
                _realizedElements ??= new();
                _measureElements ??= new();

                // We need to set the lastEstimatedElementSizeU before calling CalculateDesiredSize()
                _ = EstimateElementSizeU();

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                var viewport = CalculateMeasureViewport(items, availableSize);

                // If the viewport is disjunct then we can recycle everything.
                if (viewport.viewportIsDisjunct)
                    _realizedElements.RecycleAllElements(_recycleElement);

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeElements(items, availableSize, ref viewport);

                // Run the final measure pass if necessary.
                if (NeedsFinalMeasurePass(_measureElements.FirstIndex, _measureElements.Elements))
                {
                    var count = _measureElements.Count;

                    for (var i = 0; i < count; ++i)
                    {
                        var e = _measureElements.Elements[i]!;
                        var previous = LayoutInformation.GetPreviousMeasureConstraint(e)!.Value;

                        if (HasInfinity(previous))
                        {
                            var index = _measureElements.FirstIndex + i;
                            var constraint = GetFinalConstraint(e, index, availableSize);
                            e.Measure(constraint);
                            viewport.measuredV = Math.Max(
                                viewport.measuredV,
                                Orientation == Orientation.Horizontal ?
                                    e.DesiredSize.Height : e.DesiredSize.Width);
                        }
                    }
                }

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                TrimUnrealizedChildren();

                return CalculateDesiredSize(orientation, items.Count, viewport);
            }
            finally
            {
                _isInLayout = false;
            }
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_realizedElements is null)
                return finalSize;

            _isInLayout = true;

            try
            {
                var orientation = Orientation;
                var u = _realizedElements!.StartU;

                for (var i = 0; i < _realizedElements.Count; ++i)
                {
                    var e = _realizedElements.Elements[i];

                    if (e is not null)
                    {
                        var sizeU = _realizedElements.SizeU[i];
                        var rect = orientation == Orientation.Horizontal ?
                            new Rect(u, 0, sizeU, finalSize.Height) :
                            new Rect(0, u, finalSize.Width, sizeU);
                        rect = ArrangeElement(i + _realizedElements.FirstIndex, e, rect);
                        _scrollViewer?.RegisterAnchorCandidate(e);
                        u += orientation == Orientation.Horizontal ? rect.Width : rect.Height;
                    }
                }

                return finalSize;
            }
            finally
            {
                _isInLayout = false;
            }
        }

        /// <summary>
        ///   Gets or estimates the anchor element for the specified viewport.
        /// </summary>
        /// <param name="viewportStart">The start position of the viewport.</param>
        /// <param name="viewportEnd">The end position of the viewport.</param>
        /// <param name="itemCount">The total number of items.</param>
        /// <returns>
        ///   A tuple containing:
        ///   - The index of the anchor element
        ///   - The position of the anchor element
        /// </returns>
        /// <remarks>
        ///   This method tries to find an existing element in the specified viewport from which
        ///   element realization can start. Failing that it estimates the first element in the
        ///   viewport.
        /// </remarks>
        protected virtual (int index, double position) GetOrEstimateAnchorElementForViewport(
            double viewportStart,
            double viewportEnd,
            int itemCount)
        {
            Debug.Assert(_realizedElements is not null);

            return _realizedElements.GetOrEstimateAnchorElementForViewport(
                viewportStart,
                viewportEnd,
                itemCount,
                ref _lastEstimatedElementSizeU);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _scrollViewer = this.FindAncestorOfType<ScrollViewer>();
            
            // Subscribing to this event adds a reference to 'this' in the layout manager.
            // so this must be unsubscribed to avoid memory leaks.
            EffectiveViewportChanged += OnEffectiveViewportChanged;
            
            SubscribeToItemChanges();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _scrollViewer = null;
            
            EffectiveViewportChanged -= OnEffectiveViewportChanged;

            UnsubscribeFromItemChanges();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            RecycleAllElements();
        }

        /// <summary>
        ///   Handles changes in the effective viewport.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        ///   This method updates the presenter's viewport and invalidates the measure
        ///   if the viewport has changed.
        /// </remarks>
        protected virtual void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var vertical = Orientation == Orientation.Vertical;
            var oldViewportStart = vertical ? Viewport.Top : Viewport.Left;
            var oldViewportEnd = vertical ? Viewport.Bottom : Viewport.Right;

            // We sometimes get sent a viewport of 0,0 because the EffectiveViewportChanged event
            // is being raised when the parent control hasn't yet been arranged. This is a bug in
            // Avalonia, but we can work around it by forcing MeasureOverride to estimate the
            // viewport.
            Viewport = e.EffectiveViewport.Size == default ? 
                s_invalidViewport :
                Intersect(e.EffectiveViewport, new(Bounds.Size));

            _isWaitingForViewportUpdate = false;

            var newViewportStart = vertical ? Viewport.Top : Viewport.Left;
            var newViewportEnd = vertical ? Viewport.Bottom : Viewport.Right;

            if (!MathUtilities.AreClose(oldViewportStart, newViewportStart) ||
                !MathUtilities.AreClose(oldViewportEnd, newViewportEnd))
            {
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///   Unrealizes an element when its item is removed from the collection.
        /// </summary>
        /// <param name="element">The element to unrealize.</param>
        /// <remarks>
        ///   This method is called when an item is removed from the collection.
        ///   By default, it delegates to <see cref="TreeDataGridPresenterBase{TItem}.UnrealizeElement(Control)" />, but derived
        ///   classes can override it to provide custom behavior.
        /// </remarks>
        protected virtual void UnrealizeElementOnItemRemoved(Control element)
        {
            UnrealizeElement(element);
        }

        private void SubscribeToItemChanges()
        {
            if (!_isSubscribedToItemChanges && _items is INotifyCollectionChanged newIncc)
            {
                newIncc.CollectionChanged += OnItemsCollectionChanged;
                _isSubscribedToItemChanges = true;
            }
        }

        private void UnsubscribeFromItemChanges()
        {
            if (_isSubscribedToItemChanges && _items is INotifyCollectionChanged oldIncc)
            {
                oldIncc.CollectionChanged -= OnItemsCollectionChanged;
                _isSubscribedToItemChanges = false;
            }
        }

        private void RealizeElements(
            IReadOnlyList<TItem> items,
            Size availableSize,
            ref MeasureViewport viewport)
        {
            Debug.Assert(_measureElements is not null);
            Debug.Assert(_realizedElements is not null);
            Debug.Assert(items.Count > 0);

            var index = viewport.anchorIndex;
            var horizontal = Orientation == Orientation.Horizontal;
            var u = viewport.anchorU;

            // If the anchor element is at the beginning of, or before, the start of the viewport
            // then we can recycle all elements before it.
            if (u <= viewport.anchorU)
                _realizedElements.RecycleElementsBefore(viewport.anchorIndex, _recycleElement);

            // Start at the anchor element and move forwards, realizing elements.
            do
            {
                var e = GetOrCreateElement(items, index);
                var constraint = GetInitialConstraint(e, index, availableSize);
                var slot = MeasureElement(index, e, constraint);

                var sizeU = horizontal ? slot.Width : slot.Height;
                var sizeV = horizontal ? slot.Height : slot.Width;

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);

                u += sizeU;
                ++index;
            } while (u < viewport.viewportUEnd && index < items.Count);

            // Store the last index and end U position for the desired size calculation.
            viewport.lastIndex = index - 1;
            viewport.realizedEndU = u;

            // We can now recycle elements after the last element.
            _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

            // Next move backwards from the anchor element, realizing elements.
            index = viewport.anchorIndex - 1;
            u = viewport.anchorU;

            while (u > viewport.viewportUStart && index >= 0)
            {
                var e = GetOrCreateElement(items, index);
                var constraint = GetInitialConstraint(e, index, availableSize);
                var slot = MeasureElement(index, e, constraint);

                var sizeU = horizontal ? slot.Width : slot.Height;
                var sizeV = horizontal ? slot.Height : slot.Width;
                u -= sizeU;

                _measureElements.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }

            // We can now recycle elements before the first element.
            _realizedElements.RecycleElementsBefore(index + 1, _recycleElement);
        }

        private Size CalculateDesiredSize(Orientation orientation, int itemCount, in MeasureViewport viewport)
        {
            var sizeU = 0.0;
            var sizeV = viewport.measuredV;

            if (viewport.lastIndex >= 0)
            {
                var remaining = itemCount - viewport.lastIndex - 1;
                sizeU = viewport.realizedEndU + (remaining * EstimateElementSizeU());
            }

            return orientation == Orientation.Horizontal ? new(sizeU, sizeV) : new(sizeV, sizeU);
        }

        private MeasureViewport CalculateMeasureViewport(IReadOnlyList<TItem> items, Size availableSize)
        {
            Debug.Assert(_realizedElements is not null);

            // If the control has not yet been laid out then the effective viewport won't have been set.
            // Try to work it out from an ancestor control.
            var viewport = Viewport != s_invalidViewport ? Viewport : EstimateViewport(availableSize);

            // Get the viewport in the orientation direction.
            var viewportStart = Orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportEnd = Orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            // Get or estimate the anchor element from which to start realization. If we are
            // scrolling to an element, use that as the anchor element. Otherwise, estimate the
            // anchor element based on the current viewport.
            int anchorIndex;
            double anchorU;

            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                anchorIndex = _scrollToIndex;
                anchorU = _scrollToElement.Bounds.Top;
            }
            else
            {
                (anchorIndex, anchorU) = GetOrEstimateAnchorElementForViewport(viewportStart, viewportEnd, items.Count);
            }

            // Check if the anchor element is not within the currently realized elements.
            var disjunct = anchorIndex < _realizedElements.FirstIndex ||
                anchorIndex > _realizedElements.LastIndex;

            return new MeasureViewport
            {
                anchorIndex = anchorIndex,
                anchorU = anchorU,
                viewportUStart = viewportStart,
                viewportUEnd = viewportEnd,
                viewportIsDisjunct = disjunct,
            };
        }

        private Control GetOrCreateElement(IReadOnlyList<TItem> items, int index)
        {
            var e = GetRealizedElement(index) ??
                GetRealizedElement(index, ref _focusedIndex, ref _focusedElement) ??
                GetRealizedElement(index, ref _scrollToIndex, ref _scrollToElement) ??
                GetRecycledOrCreateElement(items, index);
            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedElements?.GetElement(index);
        }

        private static Control? GetRealizedElement(
            int index,
            ref int specialIndex,
            ref Control? specialElement)
        {
            if (specialIndex == index)
            {
                Debug.Assert(specialElement is not null);

                var result = specialElement;
                specialIndex = -1;
                specialElement = null;
                return result;
            }

            return null;
        }

        private Control GetRecycledOrCreateElement(IReadOnlyList<TItem> items, int index)
        {
            var item = items[index];
            var e = GetElementFromFactory(item, index);
            RealizeElement(e, item, index);
            e.IsVisible = true;
            if (e.GetVisualParent() is null)
            {
                ((ISetLogicalParent)e).SetParent(this);
                VisualChildren.Add(e);
            }
            return e;
        }

        private Size EstimateDesiredSize(Orientation orientation, int itemCount)
        {
            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                // We have an element to scroll to, so we can estimate the desired size based on the
                // element's position and the remaining elements.
                var remaining = itemCount - _scrollToIndex - 1;
                var u = orientation == Orientation.Horizontal ?
                    _scrollToElement.Bounds.Right :
                    _scrollToElement.Bounds.Bottom;
                var sizeU = u + (remaining * _lastEstimatedElementSizeU);
                return orientation == Orientation.Horizontal ?
                    new(sizeU, DesiredSize.Height) :
                    new(DesiredSize.Width, sizeU);
            }

            return DesiredSize;
        }

        private double EstimateElementSizeU()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeU;

            var result = _realizedElements.EstimateElementSizeU();
            if (result >= 0)
                _lastEstimatedElementSizeU = result;
            return _lastEstimatedElementSizeU;
        }

        /// <summary>
        ///   Estimates the viewport based on the available size.
        /// </summary>
        /// <param name="availableSize">The available size for measurement.</param>
        /// <returns>
        ///   The estimated viewport rectangle.
        /// </returns>
        /// <remarks>
        ///   This method is called when the actual viewport is not yet known, for example
        ///   during the first layout pass. The default implementation walks up the visual tree to
        ///   find a parent with a valid bounds, and uses that to estimate the viewport. If no such
        ///   ancestor is found, it uses the available size as the viewport size.
        /// </remarks>
        private Rect EstimateViewport(Size availableSize)
        {
            var c = this.GetVisualParent();

            if (c is null)
            {
                return default;
            }

            while (c is not null)
            {
                if (!c.Bounds.Equals(default) && c.TransformToVisual(this) is Matrix transform)
                {
                    var r = new Rect(0, 0, c.Bounds.Width, c.Bounds.Height).TransformToAABB(transform);
                    return Intersect(r, new(0, 0, double.PositiveInfinity, double.PositiveInfinity));
                }

                c = c?.GetVisualParent();
            }

            return new Rect(
                0,
                0,
                Double.IsFinite(availableSize.Width) ? availableSize.Width : 0,
                Double.IsFinite(availableSize.Height) ? availableSize.Height : 0); 
        }

        private void RecycleElement(Control element, int index)
        {
            if (element.IsKeyboardFocusWithin)
            {
                _focusedElement = element;
                _focusedIndex = index;
                _focusedElement.LostFocus += OnUnrealizedFocusedElementLostFocus;
            }
            else
            {
                element.IsVisible = false;
                UnrealizeElement(element);
                ElementFactory!.RecycleElement(element);
                _scrollViewer?.UnregisterAnchorCandidate(element);
            }
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            if (element == _focusedElement)
            {
                _focusedElement.LostFocus -= OnUnrealizedFocusedElementLostFocus;
                _focusedElement = null;
                _focusedIndex = -1;
            }

            element.IsVisible = false;
            UnrealizeElementOnItemRemoved(element);
            ElementFactory!.RecycleElement(element);
            _scrollViewer?.UnregisterAnchorCandidate(element);
        }

        private void TrimUnrealizedChildren()
        {
            var count = Items?.Count ?? 0;
            var children = VisualChildren;

            if (children.Count > count)
            {
                for (var i = children.Count - 1; i >= 0; --i)
                {
                    var child = children[i];

                    if (!child.IsVisible)
                    {
                        ((ISetLogicalParent)child).SetParent(null);
                        children.RemoveAt(i);
                    }
                }
            }
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            void ClearFocusedElement(int index, int count)
            {
                if (_focusedElement is not null && _focusedIndex >= index && _focusedIndex < index + count)
                    RecycleElementOnItemRemoved(_focusedElement);
            }

            InvalidateMeasure();

            if (_realizedElements is null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    ClearFocusedElement(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count, _recycleElementOnItemRemoved);
                    ClearFocusedElement(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    ClearFocusedElement(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _realizedElements.ItemsReset(_recycleElementOnItemRemoved);
                    if (_focusedElement is not null )
                        RecycleElementOnItemRemoved(_focusedElement);
                    break;
            }
        }

        private void OnUnrealizedFocusedElementLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_focusedElement is null || sender != _focusedElement)
                return;

            _focusedElement.LostFocus -= OnUnrealizedFocusedElementLostFocus;
            RecycleElement(_focusedElement, _focusedIndex);
            _focusedElement = null;
            _focusedIndex = -1;
        }

        private static bool HasInfinity(Size s) => double.IsInfinity(s.Width) || double.IsInfinity(s.Height);

        private static Rect Intersect(Rect a, Rect b)
        {
            // Hack fix for https://github.com/AvaloniaUI/Avalonia/issues/15075
            var newLeft = (a.X > b.X) ? a.X : b.X;
            var newTop = (a.Y > b.Y) ? a.Y : b.Y;
            var newRight = (a.Right < b.Right) ? a.Right : b.Right;
            var newBottom = (a.Bottom < b.Bottom) ? a.Bottom : b.Bottom;

            if ((newRight >= newLeft) && (newBottom >= newTop))
            {
                return new Rect(newLeft, newTop, newRight - newLeft, newBottom - newTop);
            }
            else
            {
                return default;
            }
        }

        private struct MeasureViewport
        {
            public int anchorIndex;
            public double anchorU;
            public double viewportUStart;
            public double viewportUEnd;
            public double measuredV;
            public double realizedEndU;
            public int lastIndex;
            public bool viewportIsDisjunct;
        }
    }
}
