using System;
using System.Collections.Generic;
using Arch.Core;
using Cysharp.Text;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Player;
using UnityEngine;
using UnityEngine.UIElements;
using Position = _Project.Gameplay.Features.Common.Position;

namespace _Project.Gameplay.Features.Progression.UI
{
    internal sealed class ArenaMinimapView : IDisposable
    {
        private const int RefreshIntervalMilliseconds = 150;
        private const int Columns = 64;
        private const int Rows = 32;
        private static readonly Color32 EnemyColor = new(251, 112, 117, 255);
        private static readonly Color32 PlayerColor = new(106, 232, 255, 255);
        private static readonly Color32 CameraColor = new(239, 244, 255, 215);
        private static readonly QueryDescription EnemyQuery = new QueryDescription()
            .WithAll<EnemyDefense, Position>()
            .WithNone<DeadTag, DestroySelfRequest>();
        private static readonly QueryDescription PlayerQuery = new QueryDescription()
            .WithAll<PlayerTag, Position>()
            .WithNone<DeadTag, DestroySelfRequest>();

        private readonly World _world;
        private readonly Camera _camera;
        private readonly Rect _arena;
        private readonly EnemyRunStatistics _statistics;
        private readonly VisualElement _map;
        private readonly Label _aliveLabel;
        private readonly Label _killedLabel;
        private readonly Label _escapedLabel;
        private readonly IVisualElementScheduledItem _refresh;
        private readonly bool[] _occupied = new bool[Columns * Rows];
        private readonly List<int> _visibleCells = new(Columns * Rows);
        private int _alive;
        private int _lastAlive;
        private int _lastKilled;
        private int _lastEscaped;
        private bool _hasPlayer;
        private Vector2 _player;
        private Rect _cameraFrame;

        public ArenaMinimapView(World world, Camera camera, Rect arena, EnemyRunStatistics statistics, VisualElement panel)
        {
            _world = world;
            _camera = camera;
            _arena = arena;
            _statistics = statistics;
            _map = panel.Q<VisualElement>("arenaMinimap");
            _aliveLabel = panel.Q<Label>("enemiesAliveCount");
            _killedLabel = panel.Q<Label>("enemiesKilledCount");
            _escapedLabel = panel.Q<Label>("enemiesEscapedCount");
            _map.generateVisualContent += Draw;
            _refresh = _map.schedule.Execute(RefreshSnapshot).Every(RefreshIntervalMilliseconds);
        }

        public void Dispose()
        {
            _refresh.Pause();
            _map.generateVisualContent -= Draw;
        }

        private void RefreshSnapshot()
        {
            _alive = 0;
            _hasPlayer = false;
            foreach (var cell in _visibleCells)
            {
                _occupied[cell] = false;
            }

            _visibleCells.Clear();
            _world.Query<Position>(in EnemyQuery, CollectEnemy);
            _world.Query<Position>(in PlayerQuery, CollectPlayer);
            var cameraPosition = (Vector2)_camera.transform.position;
            var halfHeight = _camera.orthographicSize;
            var halfWidth = halfHeight * _camera.aspect;
            var upperLeft = Normalize(new Vector2(cameraPosition.x - halfWidth, cameraPosition.y + halfHeight));
            var lowerRight = Normalize(new Vector2(cameraPosition.x + halfWidth, cameraPosition.y - halfHeight));
            _cameraFrame = Rect.MinMaxRect(upperLeft.x, upperLeft.y, lowerRight.x, lowerRight.y);

            UpdateCounter(_aliveLabel, _alive, ref _lastAlive);
            UpdateCounter(_killedLabel, _statistics.Killed, ref _lastKilled);
            UpdateCounter(_escapedLabel, _statistics.Escaped, ref _lastEscaped);
            _map.MarkDirtyRepaint();
        }

        private void CollectEnemy(ref Position position)
        {
            _alive++;
            var point = position.Value;
            if (point.x < _arena.xMin || point.x > _arena.xMax ||
                point.y < _arena.yMin || point.y > _arena.yMax)
            {
                return;
            }

            var normalized = Normalize(point);
            var x = Mathf.Min(Columns - 1, (int)(normalized.x * Columns));
            var y = Mathf.Min(Rows - 1, (int)(normalized.y * Rows));
            var cell = y * Columns + x;
            if (!_occupied[cell])
            {
                _occupied[cell] = true;
                _visibleCells.Add(cell);
            }
        }

        private void CollectPlayer(ref Position position)
        {
            _hasPlayer = true;
            _player = Normalize(position.Value);
        }

        private Vector2 Normalize(Vector2 point) => new(
            Mathf.InverseLerp(_arena.xMin, _arena.xMax, point.x),
            Mathf.InverseLerp(_arena.yMax, _arena.yMin, point.y));

        private static void UpdateCounter(Label label, int count, ref int previous)
        {
            if (previous == count) return;
            previous = count;
            label.text = ZString.Format("{0}", count);
        }

        private void Draw(MeshGenerationContext context)
        {
            var area = _map.contentRect;
            if (area.width <= 0f || area.height <= 0f) return;

            var quadCount = _visibleCells.Count + (_hasPlayer ? 1 : 0) + (_cameraFrame.width > 0f ? 4 : 0);
            if (quadCount == 0) return;

            var mesh = context.Allocate(quadCount * 4, quadCount * 6);
            ushort vertex = 0;
            foreach (var cell in _visibleCells)
            {
                var x = area.xMin + (cell % Columns + 0.5f) * area.width / Columns;
                var y = area.yMin + (cell / Columns + 0.5f) * area.height / Rows;
                AddQuad(mesh, ref vertex, new Rect(x - 1.25f, y - 1.25f, 2.5f, 2.5f), EnemyColor);
            }

            if (_cameraFrame.width > 0f)
            {
                var left = area.xMin + _cameraFrame.xMin * area.width;
                var right = area.xMin + _cameraFrame.xMax * area.width;
                var top = area.yMin + _cameraFrame.yMin * area.height;
                var bottom = area.yMin + _cameraFrame.yMax * area.height;
                const float line = 1.5f;
                AddQuad(mesh, ref vertex, new Rect(left, top, right - left, line), CameraColor);
                AddQuad(mesh, ref vertex, new Rect(left, bottom - line, right - left, line), CameraColor);
                AddQuad(mesh, ref vertex, new Rect(left, top, line, bottom - top), CameraColor);
                AddQuad(mesh, ref vertex, new Rect(right - line, top, line, bottom - top), CameraColor);
            }

            if (_hasPlayer)
            {
                var x = Mathf.Clamp(area.xMin + _player.x * area.width, area.xMin + 3f, area.xMax - 3f);
                var y = Mathf.Clamp(area.yMin + _player.y * area.height, area.yMin + 3f, area.yMax - 3f);
                AddQuad(mesh, ref vertex, new Rect(x - 3f, y - 3f, 6f, 6f), PlayerColor);
            }
        }

        private static void AddQuad(MeshWriteData mesh, ref ushort start, Rect rect, Color32 color)
        {
            var vertex = new Vertex { tint = color };
            vertex.position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ);
            mesh.SetNextVertex(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ);
            mesh.SetNextVertex(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ);
            mesh.SetNextVertex(vertex);
            vertex.position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ);
            mesh.SetNextVertex(vertex);
            mesh.SetNextIndex(start);
            mesh.SetNextIndex((ushort)(start + 1));
            mesh.SetNextIndex((ushort)(start + 2));
            mesh.SetNextIndex(start);
            mesh.SetNextIndex((ushort)(start + 2));
            mesh.SetNextIndex((ushort)(start + 3));
            start += 4;
        }
    }
}
