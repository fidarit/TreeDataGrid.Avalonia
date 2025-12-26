using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Models
{
    /// <summary>
    ///   Base class providing property change notification support.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This class should be considered internal to the TreeDataGrid package and is not intended
    ///     for use outside of this package.
    ///   </para>
    ///   <para>
    ///     Provides helper methods to simplify property change notification implementation.
    ///   </para>
    /// </remarks>
    public class NotifyingBase : INotifyPropertyChanged
    {
        /// <summary>
        ///   Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///   Sets the field to the specified value and raises the <see cref="PropertyChanged" /> event
        ///   if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The field storing the property's value.</param>
        /// <param name="value">The property's new value.</param>
        /// <param name="propertyName">
        ///   The name of the property. This optional parameter
        ///   can be skipped because the compiler automatically provides the property name when
        ///   called from a property setter.
        /// </param>
        /// <returns>True if the value was changed, otherwise false.</returns>
        protected bool RaiseAndSetIfChanged<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                RaisePropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Raises the <see cref="PropertyChanged" /> event.
        /// </summary>
        /// <param name="propertyName">
        ///   The name of the property that changed. This optional parameter
        ///   can be skipped because the compiler automatically provides the property name when
        ///   called from a property setter.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        ///   Raises the <see cref="PropertyChanged" /> event with the specified event args.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
