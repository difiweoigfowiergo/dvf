using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PolygonApp
{
    public class DataBaseHelper
    {
        readonly string _cs;
        public DataBaseHelper(string cs) { _cs = cs; }

        public async Task InitAsync()
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS polygons(" +
                "id SERIAL PRIMARY KEY, sides INT NOT NULL, color VARCHAR(20) NOT NULL);", c)
                .ExecuteNonQueryAsync();
        }

        public async Task<List<PolygonEntry>> AllAsync()
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            await using var r = await new NpgsqlCommand(
                "SELECT id,sides,color FROM polygons ORDER BY id;", c).ExecuteReaderAsync();
            var list = new List<PolygonEntry>();
            while (await r.ReadAsync())
                list.Add(new PolygonEntry { Id = r.GetInt32(0), Sides = r.GetInt32(1), Color = r.GetString(2) });
            return list;
        }

        public async Task<int> InsertAsync(int sides, string color)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            var cmd = new NpgsqlCommand(
                "INSERT INTO polygons(sides,color) VALUES(@s,@c) RETURNING id;", c);
            cmd.Parameters.AddWithValue("s", sides);
            cmd.Parameters.AddWithValue("c", color);
            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateColorAsync(int id, string color)
        {
            await using var c = new NpgsqlConnection(_cs);
            await c.OpenAsync();
            var cmd = new NpgsqlCommand("UPDATE polygons SET color=@c WHERE id=@id;", c);
            cmd.Parameters.AddWithValue("c", color);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public class Cmd : ICommand
    {
        readonly Action<object?> _x;
        readonly Func<object?, bool>? _can;
        public Cmd(Action<object?> x, Func<object?, bool>? can = null) { _x = x; _can = can; }
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _x(p);
    }

    public class NullToVisConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotSupportedException();
    }
}
