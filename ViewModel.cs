using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PolygonApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        DataBaseHelper? _repo;

        string _newSides    = "3";
        string _newColor    = "#000000";
        string _changeColor = "#AAAAAA";
        string _status      = "";
        PolygonEntry? _selected;
        PointCollection? _previewPoints;
        string _previewLabel = "";

        public string NewSides      { get => _newSides;    set => Set(ref _newSides,    value); }
        public string NewColor      { get => _newColor;    set => Set(ref _newColor,    value); }
        public string ChangeColor   { get => _changeColor; set => Set(ref _changeColor, value); }
        public string StatusMessage { get => _status;      set => Set(ref _status,      value); }

        // Computed geometry for the preview — View just binds, no code-behind drawing needed
        public PointCollection? PreviewPoints  { get => _previewPoints;  private set => Set(ref _previewPoints,  value); }
        public string           PreviewLabel   { get => _previewLabel;   private set => Set(ref _previewLabel,   value); }

        public PolygonEntry? SelectedPolygon
        {
            get => _selected;
            set
            {
                Set(ref _selected, value);
                if (value != null) ChangeColor = value.Color;
                UpdatePreview();
            }
        }

        public ObservableCollection<PolygonEntry> Polygons { get; } = new();

        public ICommand AddCommand         { get; }
        public ICommand ChangeColorCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        // Fixed canvas size (matches the 360×460 canvas defined in XAML)
        const double CX = 180, CY = 230, R = 160;

        public MainViewModel()
        {
            AddCommand         = new Cmd(_ => _ = DoAddAsync());
            ChangeColorCommand = new Cmd(_ => _ = DoChangeColorAsync(),
                                         _ => _selected != null && _repo != null);
            _ = TryConnectAsync();
        }

        async Task TryConnectAsync()
        {
            try
            {
                _repo = new DataBaseHelper("Host=localhost;Database=polygons_db;Username=postgres");
                await _repo.InitAsync();
                foreach (var e in await _repo.AllAsync())
                    Polygons.Add(e);
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        async Task DoAddAsync()
        {
            if (!int.TryParse(NewSides, out int s) || s < 3)
                { StatusMessage = "Sides: min 3"; return; }
            if (!IsValidColor(NewColor))
                { StatusMessage = "Invalid color"; return; }

            StatusMessage = "";
            var entry = new PolygonEntry { Sides = s, Color = NewColor };
            if (_repo != null)
            {
                try   { entry.Id = await _repo.InsertAsync(s, NewColor); }
                catch (Exception ex) { StatusMessage = ex.Message; return; }
            }
            Polygons.Add(entry);
            SelectedPolygon = entry;
        }

        async Task DoChangeColorAsync()
        {
            if (_selected == null || _repo == null) return;
            if (!IsValidColor(ChangeColor)) { StatusMessage = "Invalid color"; return; }
            StatusMessage = "";
            try
            {
                await _repo.UpdateColorAsync(_selected.Id, ChangeColor);
                _selected.Color = ChangeColor;
                UpdatePreview();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        void UpdatePreview()
        {
            var p = _selected;
            if (p == null)
            {
                PreviewPoints = null;
                PreviewLabel  = "";
                return;
            }

            var pts = new PointCollection();
            for (int i = 0; i < p.Sides; i++)
            {
                double a = -Math.PI / 2 + 2 * Math.PI * i / p.Sides;
                pts.Add(new Point(CX + R * Math.Cos(a), CY + R * Math.Sin(a)));
            }
            PreviewPoints = pts;
            PreviewLabel  = $"{p.Sides} sides";
        }

        static bool IsValidColor(string? s)
        {
            try { ColorConverter.ConvertFromString(s ?? ""); return true; }
            catch { return false; }
        }

        void Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(f, v)) return;
            f = v;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }
}
