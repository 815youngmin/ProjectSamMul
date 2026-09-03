#nullable enable
using Shared.DataTables;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;

namespace Z.GameClients.Stages
{
    public readonly struct ZoneIndex : IEquatable<ZoneIndex>
    {
        public readonly int X;
        public readonly int Y;

        public ZoneIndex(int x, int y)
        {
            X = x;
            Y = y;
        }

        public ZoneIndex(Vector2Int index) : this(index.x, index.y) { }

        public bool Equals(ZoneIndex other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is ZoneIndex other && this.Equals(other);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public static bool operator ==(ZoneIndex left, ZoneIndex right) => left.Equals(right);
        public static bool operator !=(ZoneIndex left, ZoneIndex right) => !left.Equals(right);
    }

    /// <summary>
    /// Uniform grid over the stage area, centred on the origin. Rebuilt every frame from the character list so that
    /// area queries only touch the cells overlapping the query rect instead of every character.
    /// </summary>
    public class ZoneManager
    {
        private readonly int _columns;
        private readonly int _rows;
        private readonly Vector2 _origin;   // world position of the grid's bottom-left corner
        private readonly List<Character>[] _cells;

        public Vector2 ZoneSize => new Vector2(GameConstants.ZONE_WIDTH, GameConstants.ZONE_HEIGHT);

        public ZoneManager(float width, float height)
        {
            _columns = Mathf.CeilToInt(width / ZoneSize.x) + 1;
            _rows = Mathf.CeilToInt(height / ZoneSize.y) + 1;
            _origin = -0.5f * new Vector2(_columns * ZoneSize.x, _rows * ZoneSize.y);

            _cells = new List<Character>[_columns * _rows];
            for (int i = 0; i < _cells.Length; ++i)
            {
                _cells[i] = new List<Character>();
            }
        }

        public void ComputeZone(IReadOnlyList<Character> characters)
        {
            this.ClearCells();
            foreach (var character in characters)
            {
                var index = this.GetZoneIndexFromWorldPosition(character.Pos);
                _cells[this.ToCellIndex(index)].Add(character);
            }
        }

        public void TryGetCharacters(Rect rect, in List<Character> characters)
        {
            var min = this.GetZoneIndexFromWorldPosition(rect.min);
            var max = this.GetZoneIndexFromWorldPosition(rect.max);

            for (int y = min.Y; y <= max.Y; ++y)
            {
                for (int x = min.X; x <= max.X; ++x)
                {
                    characters.AddRange(_cells[this.ToCellIndex(new ZoneIndex(x, y))]);
                }
            }
        }

        // Positions outside the stage area are clamped into the border cells.
        public ZoneIndex GetZoneIndexFromWorldPosition(Vector2 worldPosition)
        {
            var local = worldPosition - _origin;
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x / ZoneSize.x), 0, _columns - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(local.y / ZoneSize.y), 0, _rows - 1);
            return new ZoneIndex(x, y);
        }

        public void ClearBeforeChangingScene()
        {
            this.ClearCells();
        }

        private int ToCellIndex(ZoneIndex index) => index.Y * _columns + index.X;

        private void ClearCells()
        {
            foreach (var cell in _cells)
            {
                cell.Clear();
            }
        }
    }
}
