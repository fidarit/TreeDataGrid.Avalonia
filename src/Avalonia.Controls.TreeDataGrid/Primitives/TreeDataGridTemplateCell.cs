using System;
using System.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control in a <see cref="TreeDataGrid" /> that displays cell content using a data template.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridTemplateCell provides a flexible way to display and edit cell content using
    ///     data templates.
    ///   </para>
    ///   <para>
    ///     This cell type allows for rich content presentation and custom editing experiences beyond
    ///     simple text or checkbox cells. The content displayed in the cell is determined by the
    ///     <see cref="ContentTemplate" /> during normal viewing, and the <see cref="EditingTemplate" />
    ///     when the cell is in edit mode.
    ///   </para>
    /// </remarks>
    public class TreeDataGridTemplateCell : TreeDataGridEditableCell
    {
        /// <summary>
        ///   Defines the <see cref="Content" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTemplateCell, object?> ContentProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTemplateCell, object?>(
                nameof(Content),
                x => x.Content);

        /// <summary>
        ///   Defines the <see cref="ContentTemplate" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTemplateCell, IDataTemplate?> ContentTemplateProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTemplateCell, IDataTemplate?>(
                nameof(ContentTemplate),
                x => x.ContentTemplate);

        /// <summary>
        ///   Defines the <see cref="EditingTemplate" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTemplateCell, IDataTemplate?> EditingTemplateProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTemplateCell, IDataTemplate?>(
                nameof(EditingTemplate),
                x => x.EditingTemplate);

        private object? _content;
        private IDataTemplate? _contentTemplate;
        private IDataTemplate? _editingTemplate;
        private ContentPresenter? _editingContentPresenter;

        /// <summary>
        ///   Gets the content to display in the cell.
        /// </summary>
        public object? Content
        {
            get => _content;
            private set
            {
                if (SetAndRaise(ContentProperty, ref _content, value))
                    RaiseCellValueChanged();
            }
        }

        /// <summary>
        ///   Gets or sets the template used to display the cell's content when not in edit mode.
        /// </summary>
        public IDataTemplate? ContentTemplate 
        { 
            get => _contentTemplate;
            set => SetAndRaise(ContentTemplateProperty, ref _contentTemplate, value);
        }

        /// <summary>
        ///   Gets or sets the template used to display the cell's content when in edit mode.
        /// </summary>
        public IDataTemplate? EditingTemplate
        {
            get => _editingTemplate;
            set => SetAndRaise(EditingTemplateProperty, ref _editingTemplate, value);
        }

        /// <summary>
        ///   Prepares the cell for display with the specified data.
        /// </summary>
        /// <param name="factory">The element factory used to create child elements.</param>
        /// <param name="selection">The selection interaction model.</param>
        /// <param name="model">The cell's data model.</param>
        /// <param name="columnIndex">The index of the cell's column.</param>
        /// <param name="rowIndex">The index of the cell's row.</param>
        /// <inheritdoc />
        public override void Realize(
            TreeDataGridElementFactory factory,
            ITreeDataGridSelectionInteraction? selection, 
            ICell model,
            int columnIndex,
            int rowIndex)
        {
            DataContext = model;
            base.Realize(factory, selection, model, columnIndex, rowIndex);
        }

        /// <summary>
        ///   Releases resources used by the cell and prepares it for reuse.
        /// </summary>
        /// <inheritdoc />
        public override void Unrealize()
        {
            DataContext = null;
            base.Unrealize();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_editingContentPresenter is not null)
                _editingContentPresenter.LostFocus -= EditingContentPresenterLostFocus;

            _editingContentPresenter = e.NameScope.Find<ContentPresenter>("PART_EditingContentPresenter");

            if (_editingContentPresenter is not null)
            {
                _editingContentPresenter.UpdateChild();

                var focus = (IInputElement?)_editingContentPresenter.GetVisualDescendants()
                    .FirstOrDefault(x => (x as IInputElement)?.Focusable == true);
                focus?.Focus();

                _editingContentPresenter.LostFocus += EditingContentPresenterLostFocus;
            }
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (ContentTemplate is null && DataContext is TemplateCell cell)
            {
                ContentTemplate = cell.GetCellTemplate(this);
                EditingTemplate = cell.GetCellEditingTemplate?.Invoke(this);
            }
        }

        /// <inheritdoc />
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            var cell = DataContext as TemplateCell;

            // If DataContext is null, we're unrealized. Don't clear the content template for unrealized
            // cells because this will mean that when the cell is realized again the template will need
            // to be rebuilt, slowing everything down.
            if (cell is not null)
            {
                Content = cell.Value;

                if (((ILogical)this).IsAttachedToLogicalTree)
                {
                    ContentTemplate = cell.GetCellTemplate(this);
                    EditingTemplate = cell.GetCellEditingTemplate?.Invoke(this);
                }
            }
            else
            {
                Content = null;
            }
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (EndEditIfFocusLost())
            {
                base.OnLostFocus(e);
            }
        }

        private void EditingContentPresenterLostFocus(object? sender, RoutedEventArgs e) => EndEditIfFocusLost();

        private bool EndEditIfFocusLost()
        {
            if (TopLevel.GetTopLevel(this) is { } topLevel &&
                topLevel?.FocusManager?.GetFocusedElement() is Control newFocus &&
                !IsDescendent(newFocus))
            {
                EndEdit();
                return true;
            }

            return false;
        }

        private bool IsDescendent(Control c)
        {
            if (this.IsVisualAncestorOf(c))
                return true;

            // If the control is not a direct visual descendent, then check to make sure it's not
            // hosted in a popup that is a descendent of the cell.
            if (TopLevel.GetTopLevel(c)?.Parent is Control host)
                return this.IsVisualAncestorOf(host);

            return false;
        }
    }
}
