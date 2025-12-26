using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    ///   A control in a <see cref="TreeDataGrid" /> that displays cells as text.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TreeDataGridTextCell is used for displaying and editing text values in a TreeDataGrid.
    ///   </para>
    ///   <para>
    ///     This cell type provides properties to control text appearance, such as text trimming,
    ///     wrapping, and alignment. When in edit mode, it displays a <see cref="TextBox" /> that
    ///     allows the user to modify the text value.
    ///   </para>
    /// </remarks>
    public class TreeDataGridTextCell : TreeDataGridEditableCell
    {
        /// <summary>
        ///   Defines the <see cref="TextTrimming" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTextCell, TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTextCell, TextTrimming>(
                nameof(TextTrimming),
                o => o.TextTrimming);

        /// <summary>
        ///   Defines the <see cref="TextWrapping" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTextCell, TextWrapping> TextWrappingProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTextCell, TextWrapping>(
                nameof(TextWrapping),
                o => o.TextWrapping);

        /// <summary>
        ///   Defines the <see cref="Value" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTextCell, string?> ValueProperty =
            AvaloniaProperty.RegisterDirect<TreeDataGridTextCell, string?>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v);

        /// <summary>
        ///   Defines the <see cref="TextAlignment" /> property.
        /// </summary>
        public static readonly DirectProperty<TreeDataGridTextCell,TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.RegisterDirect < TreeDataGridTextCell, TextAlignment>(
                nameof(TextAlignment),
                o => o.TextAlignment,
                (o,v)=> o.TextAlignment = v);

        private string? _value;
        private TextBox? _edit;
        private bool _modelValueChanging;
        private TextTrimming _textTrimming = TextTrimming.CharacterEllipsis;
        private TextWrapping _textWrapping = TextWrapping.NoWrap;
        private TextAlignment _textAlignment = TextAlignment.Left;

        /// <summary>
        ///   Gets or sets the text trimming mode for the cell's text.
        /// </summary>
        /// <value>
        ///   The text trimming mode to apply to the cell's text.
        ///   The default is <see cref="TextTrimming.CharacterEllipsis" />.
        /// </value>
        /// <remarks>
        ///   Text trimming defines how text is trimmed when it overflows the available space.
        /// </remarks>
        public TextTrimming TextTrimming
        {
            get => _textTrimming;
            set => SetAndRaise(TextTrimmingProperty, ref _textTrimming, value);
        }

        /// <summary>
        ///   Gets or sets the text wrapping mode for the cell's text.
        /// </summary>
        /// <value>
        ///   The text wrapping mode to apply to the cell's text.
        ///   The default is <see cref="TextWrapping.NoWrap" />.
        /// </value>
        /// <remarks>
        ///   Text wrapping defines whether text wraps when it reaches the edge of its container.
        /// </remarks>
        public TextWrapping TextWrapping
        {
            get => _textWrapping;
            set => SetAndRaise(TextWrappingProperty, ref _textWrapping, value);
        }

        /// <summary>
        ///   Gets or sets the text value displayed in the cell.
        /// </summary>
        /// <remarks>
        ///   When this property changes, the new value is propagated to the underlying
        ///   model and the <see cref="TreeDataGrid.CellValueChanged" /> event is raised.
        /// </remarks>
        public string? Value
        {
            get => _value;
            set
            {
                if (SetAndRaise(ValueProperty, ref _value, value) && Model is ITextCell cell)
                {
                    if (!_modelValueChanging)
                        cell.Text = _value;
                    RaiseCellValueChanged();
                }
            }
        }

        /// <summary>
        ///   Gets or sets the text alignment for the cell's text.
        /// </summary>
        /// <value>
        ///   The text alignment to apply to the cell's text.
        ///   The default is <see cref="TextAlignment.Left" />.
        /// </value>
        /// <remarks>
        ///   Text alignment defines how text is aligned horizontally within the cell.
        /// </remarks>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set => SetAndRaise(TextAlignmentProperty, ref _textAlignment, value);
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
            Value = (model as ITextCell)?.Text;
            TextTrimming = (model as ITextCell)?.TextTrimming ?? TextTrimming.CharacterEllipsis;
            TextWrapping = (model as ITextCell)?.TextWrapping ?? TextWrapping.NoWrap;
            TextAlignment = (model as ITextCell)?.TextAlignment ?? TextAlignment.Left;
            base.Realize(factory, selection, model, columnIndex, rowIndex);
        }

        /// <inheritdoc />
        protected override void UpdateValue()
        {
            Value = (Model as ITextCell)?.Text;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _edit = e.NameScope.Find<TextBox>("PART_Edit");

            if (_edit is not null)
            {
                _edit.SelectAll();
                _edit.Focus();
            }
        }

        /// <inheritdoc />
        protected override void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChanged(sender, e);

            if (e.PropertyName == nameof(ITextCell.Value))
            {
                try
                {
                    _modelValueChanging = true;
                    UpdateValue();
                }
                finally
                {
                    _modelValueChanging = false;
                }
            }
        }
    }
}
