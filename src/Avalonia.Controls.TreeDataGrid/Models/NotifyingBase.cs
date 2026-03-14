using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Models
{
    /// <summary>
    /// Base class that implements <see cref="INotifyPropertyChanged"/> and provides helpers
    /// for derived view-model and model classes.
    /// </summary>
    public class NotifyingBase : INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Sets field and raises PropertyChanged if value changed.</summary>
        /// <returns>true if changed, false if values were equal</returns>
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

        /// <summary>Raises PropertyChanged for the calling property.</summary>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Raises PropertyChanged with custom event args.</summary>
        protected void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
