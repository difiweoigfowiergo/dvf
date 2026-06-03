using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PolygonApp
{
    public class PolygonEntry : INotifyPropertyChanged
    {
        private string _color = "#000000";
        public int Id { get; set; }
        public int Sides { get; set; }
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
