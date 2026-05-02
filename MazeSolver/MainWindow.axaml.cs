using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MazeSolver {
    public partial class MainWindow: Window {
        private int _gridSize;
        private int[, ] _maze;
        private Border[, ] _uiCells1;
        private Border[, ] _uiCells2;

        private DispatcherTimer _animationTimer;
        private Stopwatch _stopwatchBfs = new Stopwatch();
        private Stopwatch _stopwatchDfs = new Stopwatch();

        // Algos
        private BfsSolver ? _bfsSolver;
        private DfsSolver ? _dfsSolver;

        public MainWindow() {
            InitializeComponent();
            _animationTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(3)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            GenerateNewMaze();
        }

        private void OnGenerateClick(object ? sender, RoutedEventArgs e) => GenerateNewMaze();
        private void OnStopClick(object ? sender, RoutedEventArgs e) {
            _animationTimer.Stop();
            _stopwatchBfs.Stop();
            _stopwatchDfs.Stop();
        }

        private void GenerateNewMaze() {
            _animationTimer.Stop();
            _stopwatchBfs.Reset();
            _stopwatchDfs.Reset();

            TimerTextBfs.Text = "BFS: 0.00s | 0 Steps";
            TimerTextDfs.Text = "DFS: 0.00s | 0 Steps";

            _gridSize = (int) DifficultySlider.Value;

            do {
                GenerateRandomData();
            } while (!IsSolvable());
            DrawGrids();
        }

        private void GenerateRandomData() {
            _maze = new int[_gridSize, _gridSize];
            Random rand = new Random();
            for (int y = 0; y < _gridSize; y++)
                for (int x = 0; x < _gridSize; x++) {
                    bool isWall = rand.NextDouble() < 0.3;
                    if ((x == 0 && y == 0) || (x == _gridSize - 1 && y == _gridSize - 1)) isWall = false;
                    _maze[x, y] = isWall ? 1 : 0;
                }
        }

        private bool IsSolvable() {
            var queue = new Queue < MazePoint > ();
            var visited = new HashSet < MazePoint > ();
            var start = new MazePoint(0, 0);

            queue.Enqueue(start);
            visited.Add(start);
            MazePoint[] dirs = {
                new MazePoint(0, -1),
                new MazePoint(0, 1),
                new MazePoint(-1, 0),
                new MazePoint(1, 0)
            };

            while (queue.Count > 0) {
                var curr = queue.Dequeue();
                if (curr.X == _gridSize - 1 && curr.Y == _gridSize - 1) return true;

                foreach(var d in dirs) {
                    MazePoint n = new MazePoint(curr.X + d.X, curr.Y + d.Y);
                    if (n.X >= 0 && n.X < _gridSize && n.Y >= 0 && n.Y < _gridSize && _maze[n.X, n.Y] == 0 && !visited.Contains(n)) {
                        visited.Add(n);
                        queue.Enqueue(n);
                    }
                }
            }
            return false;
        }

        private void DrawGrids() {
            _uiCells1 = new Border[_gridSize, _gridSize];
            _uiCells2 = new Border[_gridSize, _gridSize];

            MazeGrid1.Rows = _gridSize;
            MazeGrid1.Columns = _gridSize;
            MazeGrid2.Rows = _gridSize;
            MazeGrid2.Columns = _gridSize;
            MazeGrid1.Children.Clear();
            MazeGrid2.Children.Clear();

            for (int y = 0; y < _gridSize; y++)
                for (int x = 0; x < _gridSize; x++) {
                    bool isWall = _maze[x, y] == 1;
                    var brush = isWall ? Brushes.DarkSlateGray : Brushes.White;
                    if (x == 0 && y == 0) brush = Brushes.Green;
                    if (x == _gridSize - 1 && y == _gridSize - 1) brush = Brushes.Red;

                    var cell1 = new Border {
                        Background = brush, BorderBrush = Brushes.Gray, BorderThickness = new Avalonia.Thickness(0.5)
                    };
                    var cell2 = new Border {
                        Background = brush, BorderBrush = Brushes.Gray, BorderThickness = new Avalonia.Thickness(0.5)
                    };

                    _uiCells1[x, y] = cell1;
                    _uiCells2[x, y] = cell2;
                    MazeGrid1.Children.Add(cell1);
                    MazeGrid2.Children.Add(cell2);
                }
        }

        private void OnStartClick(object ? sender, RoutedEventArgs e) {
            _animationTimer.Stop();

            // Clear old paths
            for (int y = 0; y < _gridSize; y++)
                for (int x = 0; x < _gridSize; x++)
                    if (_maze[x, y] == 0 && !(x == 0 && y == 0) && !(x == _gridSize - 1 && y == _gridSize - 1)) {
                        _uiCells1[x, y].Background = Brushes.White;
                        _uiCells2[x, y].Background = Brushes.White;
                    }

            MazePoint start = new MazePoint(0, 0);
            int mode = AlgorithmCombo.SelectedIndex;

            _bfsSolver = null;
            _dfsSolver = null;
            _stopwatchBfs.Reset();
            _stopwatchDfs.Reset();

            if (mode == 0 || mode == 2) {
                _bfsSolver = new BfsSolver(start);
                _stopwatchBfs.Start();
            }

            if (mode == 1 || mode == 2) {
                _dfsSolver = new DfsSolver(start);
                _stopwatchDfs.Start();
            }

            // Adjust Layout
            if (mode == 2) {
                RightMazeContainer.IsVisible = true;
                MazeSplitter.IsVisible = true;
                Grid.SetColumnSpan(LeftMazeContainer, 1);
                Maze1Title.Text = "BFS Visualization";
                Maze2Title.Text = "DFS Visualization";
                TimerTextBfs.IsVisible = true;
                TimerTextDfs.IsVisible = true;
            } else {
                RightMazeContainer.IsVisible = false;
                MazeSplitter.IsVisible = false;
                Grid.SetColumnSpan(LeftMazeContainer, 3);
                Maze1Title.Text = mode == 0 ? "BFS Visualization" : "DFS Visualization";
                TimerTextBfs.IsVisible = mode == 0;
                TimerTextDfs.IsVisible = mode == 1;
            }

            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object ? sender, EventArgs e) {
            int mode = AlgorithmCombo.SelectedIndex;
            int dfsGridIndex = mode == 2 ? 2 : 1;

            // --- BFS STEP ---
            if (_bfsSolver != null && !_bfsSolver.IsDone) {
                TimerTextBfs.Text = $"BFS: {_stopwatchBfs.Elapsed.TotalSeconds:F2}s | {_bfsSolver.Steps} Steps";

                // Pass a callback to color the newly added neighbors yellow
                MazePoint ? current = _bfsSolver.Step(_maze, _gridSize, neighbor => ColorCell(neighbor, Brushes.Yellow, 1));

                if (current.HasValue) {
                    if (current.Value.X == _gridSize - 1 && current.Value.Y == _gridSize - 1) {
                        DrawPath(current.Value, _bfsSolver.CameFrom, Brushes.Cyan, 1);
                        _stopwatchBfs.Stop();
                        TimerTextBfs.Text = $"BFS: {_stopwatchBfs.Elapsed.TotalSeconds:F2}s | {_bfsSolver.Steps} Steps (Done)";
                    } else {
                        ColorCell(current.Value, Brushes.LightBlue, 1);
                    }
                } else {
                    _stopwatchBfs.Stop();
                    TimerTextBfs.Text = $"BFS: {_stopwatchBfs.Elapsed.TotalSeconds:F2}s | {_bfsSolver.Steps} Steps (No Path)";
                }
            }

            // --- DFS STEP ---
            if (_dfsSolver != null && !_dfsSolver.IsDone) {
                TimerTextDfs.Text = $"DFS: {_stopwatchDfs.Elapsed.TotalSeconds:F2}s | {_dfsSolver.Steps} Steps";

                // Pass a callback to color the newly added neighbors orange
                MazePoint ? current = _dfsSolver.Step(_maze, _gridSize, neighbor => ColorCell(neighbor, Brushes.Orange, dfsGridIndex));

                if (current.HasValue) {
                    if (current.Value.X == _gridSize - 1 && current.Value.Y == _gridSize - 1) {
                        DrawPath(current.Value, _dfsSolver.CameFrom, Brushes.Magenta, dfsGridIndex);
                        _stopwatchDfs.Stop();
                        TimerTextDfs.Text = $"DFS: {_stopwatchDfs.Elapsed.TotalSeconds:F2}s | {_dfsSolver.Steps} Steps (Done)";
                    } else {
                        ColorCell(current.Value, Brushes.LightPink, dfsGridIndex);
                    }
                } else {
                    _stopwatchDfs.Stop();
                    TimerTextDfs.Text = $"DFS: {_stopwatchDfs.Elapsed.TotalSeconds:F2}s | {_dfsSolver.Steps} Steps (No Path)";
                }
            }

            bool bfsFinished = _bfsSolver == null || _bfsSolver.IsDone;
            bool dfsFinished = _dfsSolver == null || _dfsSolver.IsDone;

            if (bfsFinished && dfsFinished) _animationTimer.Stop();
        }

        private void ColorCell(MazePoint p, ISolidColorBrush brush, int gridIndex) {
            if ((p.X == 0 && p.Y == 0) || (p.X == _gridSize - 1 && p.Y == _gridSize - 1)) return;
            if (gridIndex == 1) _uiCells1[p.X, p.Y].Background = brush;
            else _uiCells2[p.X, p.Y].Background = brush;
        }

        private void DrawPath(MazePoint endNode, Dictionary < MazePoint, MazePoint > cameFrom, ISolidColorBrush pathColor, int gridIndex) {
            MazePoint current = endNode;
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                ColorCell(current, pathColor, gridIndex);
            }
        }
    }
}