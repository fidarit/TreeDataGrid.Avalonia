using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   Factory class responsible for creating and recycling UI elements used in the
    ///   <see cref="TreeDataGrid" /> control.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The TreeDataGridElementFactory manages the creation of UI elements in the TreeDataGrid,
    ///     including rows, cells, and column headers. It implements an element recycling strategy
    ///     that improves performance by reusing existing UI elements rather than creating new ones
    ///     whenever possible.
    ///   </para>
    ///   <para>
    ///     This class can be extended to customize the creation and recycling of TreeDataGrid elements
    ///     by overriding the <see cref="CreateElement(object?)" />, <see cref="GetDataRecycleKey(object?)" />, and
    ///     <see cref="GetElementRecycleKey(Control)" /> methods.
    ///   </para>
    /// </remarks>
    public class TreeDataGridElementFactory
    {
        private readonly Dictionary<object, List<Control>>  _recyclePool = [];

        /// <summary>
        ///   Gets an existing recycled element or creates a new element for the specified data.
        /// </summary>
        /// <param name="data">
        ///   The TreeDataGrid model for which to get or create an element, e.g. <see cref="ICell" />
        ///   or <see cref="CheckBoxCell" />.
        /// </param>
        /// <param name="parent">
        ///   The parent control that will host the element, e.g. a <see cref="TreeDataGridRow" />.
        /// </param>
        /// <returns>A control that can be used to display the specified data.</returns>
        /// <remarks>
        ///   <para>
        ///     This method first attempts to reuse an existing element from the recycle pool. It
        ///     tries to find an element with the same parent, then an element with no parent, and
        ///     finally creates a new element if necessary.
        ///   </para>
        ///   <para>
        ///     The returned element will be ready to use but may need to be further configured with
        ///     specific data, for example the DataContext is not set by this method.
        ///   </para>
        /// </remarks>
        public Control GetOrCreateElement(object? data, Control parent)
        {
            var recycleKey = GetDataRecycleKey(data);

            if (_recyclePool.TryGetValue(recycleKey, out var elements) && elements.Count > 0)
            {
                // First look for an element with the same parent.
                for (var i = 0; i < elements.Count; i++)
                { 
                    var e = elements[i];

                    if (e.Parent == parent)
                    {
                        parent.InvalidateMeasure();
                        elements.RemoveAt(i);
                        return e;
                    }
                }

                // Next look for an element with no parent or an element that we can reparent.
                for (var i = 0; i < elements.Count; i++)
                {
                    var e = elements[i];
                    var parentPanel = e.Parent as Panel;

                    if (e.Parent is null || parentPanel is not null)
                    {
                        parent.InvalidateMeasure();
                        parentPanel?.Children.Remove(e);
                        Debug.Assert(e.Parent is null);
                        elements.RemoveAt(i);
                        return e;
                    }
                }
            }

            // Otherwise create a new element.
            return CreateElement(data);
        }

        /// <summary>
        ///   Recycles an element for later reuse.
        /// </summary>
        /// <param name="element">The control element to recycle.</param>
        /// <remarks>
        ///   <para>
        ///     When an element is no longer needed (e.g., when it scrolls out of view), this method
        ///     should be called to add it to the recycle pool. The element can then be reused later
        ///     when a similar element is needed, improving performance by reducing the need to create
        ///     new elements.
        ///   </para>
        ///   <para>
        ///     Recycled elements should be in a clean state, with any data-specific state cleared.
        ///   </para>
        /// </remarks>
        public void RecycleElement(Control element)
        {
            var recycleKey = GetElementRecycleKey(element);

            if (!_recyclePool.TryGetValue(recycleKey, out var elements))
            {
                elements = [];
                _recyclePool.Add(recycleKey, elements);
            }

            elements.Add(element);
        }

        /// <summary>
        ///   Creates a new element for the specified data.
        /// </summary>
        /// <param name="data">
        ///   The TreeDataGrid model for which to get or create an element, e.g. <see cref="ICell" />
        ///   or <see cref="CheckBoxCell" />.
        /// </param>
        /// <returns>A control that can be used to display the specified data.</returns>
        /// <exception cref="NotSupportedException">
        ///   Thrown when the type of <paramref name="data" /> is not supported.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This method is responsible for creating new UI elements based on the type of the data
        ///     model. The base implementation creates appropriate control types for inbuilt cell types,
        ///     rows, and column headers.
        ///   </para>
        ///   <para>
        ///     Override this method in derived classes to customize the element creation process or
        ///     to support additional data types.
        ///   </para>
        /// </remarks>
        protected virtual Control CreateElement(object? data)
        {
            return data switch
            {
                CheckBoxCell => new TreeDataGridCheckBoxCell(),
                TemplateCell => new TreeDataGridTemplateCell(),
                IExpanderCell => new TreeDataGridExpanderCell(),
                ICell => new TreeDataGridTextCell(),
                IColumn => new TreeDataGridColumnHeader(),
                IRow => new TreeDataGridRow(),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        ///   Gets the recycle key for the specified data model.
        /// </summary>
        /// <param name="data">
        ///   The TreeDataGrid model for which to get a recycle key, e.g. <see cref="ICell" /> or
        ///   <see cref="CheckBoxCell" />.
        /// </param>
        /// <returns>A string that identifies the type of element needed for the data.</returns>
        /// <exception cref="NotSupportedException">
        ///   Thrown when the data type is not supported.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     The recycle key is used to identify which recycled elements can be used for a
        ///     particular data model. By default, when a supported data type is passed in, the
        ///     <see cref="Type.FullName" /> of the <paramref name="data" /> is returned.
        ///   </para>
        ///   <para>
        ///     Override this method in derived classes to customize the recycling behavior or to
        ///     support additional data types.
        ///   </para>
        /// </remarks>
        protected virtual string GetDataRecycleKey(object? data)
        {
            return data switch
            {
                CheckBoxCell => typeof(TreeDataGridCheckBoxCell).FullName!,
                TemplateCell => typeof(TreeDataGridTemplateCell).FullName!,
                IExpanderCell => typeof(TreeDataGridExpanderCell).FullName!,
                ICell => typeof(TreeDataGridTextCell).FullName!,
                IColumn => typeof(TreeDataGridColumnHeader).FullName!,
                IRow => typeof(TreeDataGridRow).FullName!,
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        ///   Gets the recycle key for the specified element.
        /// </summary>
        /// <param name="element">The control element for which to get a recycle key.</param>
        /// <returns>A string that identifies the type of the element.</returns>
        /// <remarks>
        ///   <para>
        ///     The recycle key is used to categorize recycled elements so they can be matched with
        ///     appropriate data models later. By default, this is based on the full name of the element's type.
        ///   </para>
        ///   <para>
        ///     Override this method in derived classes to customize the recycling behavior.
        ///   </para>
        /// </remarks>
        protected virtual string GetElementRecycleKey(Control element)
        {
            return element.GetType().FullName!;
        }
    }
}
