using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MazeSolver {
    public partial class MainWindow: Window {
        private int _gridSize;
        private int[, ] _maze;

        private Border[][, ] _uiCells = new Border[4][, ];
        private IMazeSolver ? [] _solvers = new IMazeSolver ? [4];
        private Stopwatch[] _stopwatches = {
            new Stopwatch(),
            new Stopwatch(),
            new Stopwatch(),
            new Stopwatch()
        };

        // UI References
        private UniformGrid[] _mazeGrids;
        private DockPanel[] _mazeContainers;
        private Border[] _mazeBorders;
        private TextBlock[] _timerTexts;
        private TextBlock[] _titles;
        private ComboBox[] _algoCombos;

        private readonly ISolidColorBrush[] _pathColors = {
            Brushes.Cyan,
            Brushes.Magenta,
            Brushes.Lime,
            Brushes.BlueViolet
        };
        private readonly ISolidColorBrush[] _exploreColors = {
            Brushes.LightBlue,
            Brushes.LightCoral,
            Brushes.LightGreen,
            Brushes.LightGray
        };
        private readonly ISolidColorBrush _defaultBorderBrush = SolidColorBrush.Parse("#555555");

        private int _activeGrids = 2;
        private DispatcherTimer _animationTimer;
        private CancellationTokenSource? _cancelTokenSource;
        private readonly string[][] _algoCodeSnippets = {
            new[] { // 0: BFS
                "while (queue.Count > 0) {", 
                "    Node current = queue.Dequeue();", 
                "    if (current == target)", "        return Path;", "", 
                "    foreach (Node n in current.Neighbors) {", 
                "        if (!visited.Contains(n)) {", 
                "            visited.Add(n);", 
                "            queue.Enqueue(n);", 
                "        }", "    }", "}"
            },
            new[] { // 1: DFS
                "while (stack.Count > 0) {", 
                "    Node current = stack.Pop();", 
                "    if (current == target)", "        return Path;", "", 
                "    foreach (Node n in current.Neighbors) {", 
                "        if (!visited.Contains(n)) {", 
                "            visited.Add(n);", 
                "            stack.Push(n);", 
                "        }", "    }", "}"
            },
            new[] { // 2: Dijkstra
                "while (pq.Count > 0) {", 
                "    Node current = pq.Dequeue();", 
                "    if (current == target)", "        return Path;", "", 
                "    foreach (Node n in current.Neighbors) {", 
                "        int newCost = cost[current] + 1;", 
                "        if (newCost < cost[n]) {", 
                "            cost[n] = newCost;", 
                "            pq.Enqueue(n, newCost);", 
                "        }", "    }", "}"
            },
            new[] { // 3: A* Search
                "while (pq.Count > 0) {", 
                "    Node current = pq.Dequeue();", 
                "    if (current == target)", "        return Path;", "", 
                "    foreach (Node n in current.Neighbors) {", 
                "        int newCost = gScore[current] + 1;", 
                "        if (newCost < gScore[n]) {", 
                "            gScore[n] = newCost;", 
                "            int fScore = newCost + Heuristic(n);", 
                "            pq.Enqueue(n, fScore);", 
                "        }", "    }", "}"
            }
        };

        public MainWindow() {
            InitializeComponent();

            _mazeGrids = new [] {
                MazeGrid1,
                MazeGrid2,
                MazeGrid3,
                MazeGrid4
            };
            _mazeContainers = new [] {
                Container1,
                Container2,
                Container3,
                Container4
            };
            _mazeBorders = new [] {
                MazeBorder1,
                MazeBorder2,
                MazeBorder3,
                MazeBorder4
            }; // Hooking up the borders
            _timerTexts = new [] {
                Timer1,
                Timer2,
                Timer3,
                Timer4
            };
            _titles = new [] {
                Title1,
                Title2,
                Title3,
                Title4
            };
            _algoCombos = new [] {
                Algo1,
                Algo2,
                Algo3,
                Algo4
            };

            _animationTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(1)
            };
            _animationTimer.Tick += AnimationTimer_Tick;

            GenerateNewMaze();
            UpdateLayoutMode();
        }

        private void OnGenerateClick(object ? sender, RoutedEventArgs e) => GenerateNewMaze();
        private void OnStopClick(object ? sender, RoutedEventArgs e) {
            _animationTimer.Stop();
            foreach(var sw in _stopwatches) sw.Stop();
            _cancelTokenSource?.Cancel();
        }

        private void OnLayoutChanged(object ? sender, SelectionChangedEventArgs e) {
            if (LayoutCombo == null) return;
            UpdateLayoutMode();
        }

        private void UpdateLayoutMode() {
            bool isExplainMode = LayoutCombo.SelectedIndex == 3;

            // If Explain Mode, force 1 Grid. Otherwise, read the dropdown.
            _activeGrids = isExplainMode ? 1 : (LayoutCombo.SelectedIndex
                switch {
                    0 => 1, 1 => 2, _ => 4
                });

            MasterGrid.Rows = _activeGrids <= 2 ? 1 : 2;
            MasterGrid.Columns = _activeGrids == 1 ? 1 : 2;

            MainSplitGrid.ColumnDefinitions[1].Width = isExplainMode ? new GridLength(380) : new GridLength(0);
            CodeContainer.IsVisible = isExplainMode;

            for (int i = 0; i < 4; i++) {
                bool active = i < _activeGrids;
                _mazeContainers[i].IsVisible = active;
                _timerTexts[i].IsVisible = active;

                if (i == 1) PanelAlgo2.IsVisible = active && !isExplainMode;
                if (i == 2) PanelAlgo3.IsVisible = active && !isExplainMode;
                if (i == 3) PanelAlgo4.IsVisible = active && !isExplainMode;
            }

            if (isExplainMode) CodeViewer.ItemsSource = _algoCodeSnippets[Algo1.SelectedIndex];
        }

        private void GenerateNewMaze() {
            _animationTimer.Stop();
            for (int i = 0; i < 4; i++) {
                _stopwatches[i].Reset();
                _mazeBorders[i].BorderBrush = _defaultBorderBrush; // Reset border color on new maze
            }

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
            var q = new Queue < MazePoint > ();
            var visited = new HashSet < MazePoint > ();
            var start = new MazePoint(0, 0);

            q.Enqueue(start);
            visited.Add(start);
            MazePoint[] dirs = {
                new MazePoint(0, -1),
                new MazePoint(0, 1),
                new MazePoint(-1, 0),
                new MazePoint(1, 0)
            };

            while (q.Count > 0) {
                var curr = q.Dequeue();
                if (curr.X == _gridSize - 1 && curr.Y == _gridSize - 1) return true;

                foreach(var d in dirs) {
                    MazePoint n = new MazePoint(curr.X + d.X, curr.Y + d.Y);
                    if (n.X >= 0 && n.X < _gridSize && n.Y >= 0 && n.Y < _gridSize && _maze[n.X, n.Y] == 0 && !visited.Contains(n)) {
                        visited.Add(n);
                        q.Enqueue(n);
                    }
                }
            }
            return false;
        }

        private void DrawGrids() {
            for (int i = 0; i < 4; i++) {
                _uiCells[i] = new Border[_gridSize, _gridSize];
                _mazeGrids[i].Rows = _gridSize;
                _mazeGrids[i].Columns = _gridSize;
                _mazeGrids[i].Children.Clear();
            }

            for (int y = 0; y < _gridSize; y++) {
                for (int x = 0; x < _gridSize; x++) {
                    bool isWall = _maze[x, y] == 1;
                    var brush = isWall ? Brushes.DarkSlateGray : Brushes.White;
                    if (x == 0 && y == 0) brush = Brushes.Green;
                    if (x == _gridSize - 1 && y == _gridSize - 1) brush = Brushes.Red;

                    for (int i = 0; i < 4; i++) {
                        var cell = new Border {
                            Background = brush, BorderBrush = Brushes.Gray, BorderThickness = new Avalonia.Thickness(0.5)
                        };
                        _uiCells[i][x, y] = cell;
                        _mazeGrids[i].Children.Add(cell);
                    }
                }
            }
        }

        private IMazeSolver CreateSolver(int typeIndex, MazePoint start) => typeIndex
        switch {
            0 => new BfsSolver(start),
                1 => new DfsSolver(start),
                2 => new DijkstraSolver(start),
                _ => new AStarSolver(start, _gridSize)
        };

        private void OnStartClick(object ? sender, RoutedEventArgs e) {
            _animationTimer.Stop();
            _cancelTokenSource?.Cancel();
            _cancelTokenSource = new CancellationTokenSource();

            for (int i = 0; i < _activeGrids; i++) {
                _timerTexts[i].Text = $"Grid {i + 1}: 0.0s | 0 Steps";
                _titles[i].Text = ((ComboBoxItem) _algoCombos[i].SelectedItem).Content.ToString();

                _solvers[i] = CreateSolver(_algoCombos[i].SelectedIndex, new MazePoint(0, 0));
                _stopwatches[i].Restart();

                _mazeBorders[i].BorderBrush = _defaultBorderBrush; // Reset border color on run start

                for (int y = 0; y < _gridSize; y++)
                    for (int x = 0; x < _gridSize; x++)
                        if (_maze[x, y] == 0 && !(x == 0 && y == 0) && !(x == _gridSize - 1 && y == _gridSize - 1))
                            _uiCells[i][x, y].Background = Brushes.White;
            }

            if (LayoutCombo.SelectedIndex == 3) CodeViewer.ItemsSource = _algoCodeSnippets[Algo1.SelectedIndex];
            _animationTimer.Start();
        }

        private async void AnimationTimer_Tick(object? sender, EventArgs e) {
            _animationTimer.Stop(); 
            
            // 1. Grab the token so we can pass it to the delays
            if (_cancelTokenSource == null) return;
            var token = _cancelTokenSource.Token;

            try {
                bool allDone = true;
                bool isExplainMode = LayoutCombo.SelectedIndex == 3;
                int delayMs = 300;

                for (int i = 0; i < _activeGrids; i++) {
                    if (_solvers[i] != null && !_solvers[i].IsDone) {
                        allDone = false;
                        _timerTexts[i].Text = $"{_titles[i].Text}: {_stopwatches[i].Elapsed.TotalSeconds:F2}s | {_solvers[i].Steps} Steps";

                        // 2. Pass 'token' into EVERY Task.Delay
                        if (isExplainMode && i == 0) {
                            CodeViewer.SelectedIndex = 0; await Task.Delay(delayMs, token); // Highlight: while loop
                            CodeViewer.SelectedIndex = 2; await Task.Delay(delayMs, token); // Highlight: Dequeue/Pop
                            CodeViewer.SelectedIndex = 3; await Task.Delay(delayMs, token); // Highlight: Check target
                        }

                        MazePoint? current = _solvers[i].Step(_maze, _gridSize, neighbor => ColorCell(neighbor, Brushes.Yellow, i));

                        if (current.HasValue) {
                            bool isTarget = current.Value.X == _gridSize - 1 && current.Value.Y == _gridSize - 1;

                            // --- EXPLAIN MODE: Bottom half of the loop ---
                            if (isExplainMode && i == 0) {
                                if (isTarget) {
                                    CodeViewer.SelectedIndex = 4; await Task.Delay(delayMs, token); // Highlight: return Path;
                                } else {
                                    CodeViewer.SelectedIndex = 6; await Task.Delay(delayMs, token); // Highlight: foreach neighbor
                                    
                                    int algo = Algo1.SelectedIndex;
                                    if (algo == 0 || algo == 1) { // BFS or DFS
                                        CodeViewer.SelectedIndex = 8; await Task.Delay(delayMs, token);  // if (!visited)
                                        CodeViewer.SelectedIndex = 10; await Task.Delay(delayMs, token); // visited.Add
                                        CodeViewer.SelectedIndex = 11; await Task.Delay(delayMs, token); // queue.Enqueue
                                    } 
                                    else if (algo == 2) { // Dijkstra
                                        CodeViewer.SelectedIndex = 8; await Task.Delay(delayMs, token);  // int newCost
                                        CodeViewer.SelectedIndex = 9; await Task.Delay(delayMs, token);  // if (newCost < cost)
                                        CodeViewer.SelectedIndex = 11; await Task.Delay(delayMs, token); // cost[n] = newCost
                                        CodeViewer.SelectedIndex = 12; await Task.Delay(delayMs, token); // pq.Enqueue
                                    } 
                                    else { // A* Search
                                        CodeViewer.SelectedIndex = 8; await Task.Delay(delayMs, token);  // int newCost
                                        CodeViewer.SelectedIndex = 9; await Task.Delay(delayMs, token);  // if (newCost < gScore)
                                        CodeViewer.SelectedIndex = 11; await Task.Delay(delayMs, token); // gScore[n] = newCost
                                        CodeViewer.SelectedIndex = 12; await Task.Delay(delayMs, token); // fScore = ...
                                        CodeViewer.SelectedIndex = 13; await Task.Delay(delayMs, token); // pq.Enqueue
                                    }
                                }
                            }

                            if (isTarget) {
                                var path = ReconstructPath(current.Value, _solvers[i].CameFrom);
                                DrawFinalPath(path, _pathColors[i], i);
                                _mazeBorders[i].BorderBrush = _pathColors[i];

                                _stopwatches[i].Stop();
                                _timerTexts[i].Text = $"{_titles[i].Text}: {_stopwatches[i].Elapsed.TotalSeconds:F2}s | {_solvers[i].Steps} Steps | {CountTurns(path)} Turns";
                            } else {
                                ColorCell(current.Value, _exploreColors[i], i);
                            }
                        } else {
                            if (isExplainMode && i == 0) CodeViewer.SelectedIndex = 0; 
                            
                            _stopwatches[i].Stop();
                            _timerTexts[i].Text += " (Failed)";
                            _mazeBorders[i].BorderBrush = Brushes.Red; 
                        }
                    }
                }

                if (!allDone) _animationTimer.Start();
                
            } 
            catch (TaskCanceledException) {
                return;
            }
        }

        private List < MazePoint > ReconstructPath(MazePoint endNode, Dictionary < MazePoint, MazePoint > cameFrom) {
            var path = new List < MazePoint > {
                endNode
            };
            var current = endNode;
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private int CountTurns(List < MazePoint > path) {
            if (path == null || path.Count < 3) return 0;
            int turns = 0;
            for (int i = 2; i < path.Count; i++) {
                int dx1 = path[i - 1].X - path[i - 2].X, dy1 = path[i - 1].Y - path[i - 2].Y;
                int dx2 = path[i].X - path[i - 1].X, dy2 = path[i].Y - path[i - 1].Y;
                if (dx1 != dx2 || dy1 != dy2) turns++;
            }
            return turns;
        }

        private void DrawFinalPath(List < MazePoint > path, ISolidColorBrush color, int gridIndex) {
            foreach(var p in path) ColorCell(p, color, gridIndex);
        }

        private void ColorCell(MazePoint p, ISolidColorBrush brush, int gridIndex) {
            if ((p.X == 0 && p.Y == 0) || (p.X == _gridSize - 1 && p.Y == _gridSize - 1)) return;
            _uiCells[gridIndex][p.X, p.Y].Background = brush;
        }
    }
}