using System;
using System.Collections.Generic;

namespace MazeSolver {
    public class AStarSolver: IMazeSolver {
        private PriorityQueue < MazePoint, int > _pq;
        private Dictionary < MazePoint, int > _gScore;
        public Dictionary < MazePoint, MazePoint > CameFrom {
            get;
        }
        public bool IsDone {
            get;
            private set;
        }
        public int Steps {
            get;
            private set;
        }
        private int _targetX, _targetY;

        public AStarSolver(MazePoint start, int gridSize) {
            _targetX = gridSize - 1;
            _targetY = gridSize - 1;

            _pq = new PriorityQueue < MazePoint, int > ();
            _pq.Enqueue(start, 0);

            _gScore = new Dictionary < MazePoint, int > {
                [start] = 0
            };
            CameFrom = new Dictionary < MazePoint, MazePoint > ();
            IsDone = false;
            Steps = 0;
        }

        // Heuristic: Manhattan Distance
        private int GetHeuristic(MazePoint p) => Math.Abs(p.X - _targetX) + Math.Abs(p.Y - _targetY);

        public MazePoint ? Step(int[, ] maze, int gridSize, Action < MazePoint > onNeighborAdded) {
            if (_pq.Count == 0) {
                IsDone = true;
                return null;
            }

            MazePoint current = _pq.Dequeue();
            Steps++;

            if (current.X == gridSize - 1 && current.Y == gridSize - 1) {
                IsDone = true;
                return current;
            }

            MazePoint[] dirs = {
                new MazePoint(0, -1),
                new MazePoint(0, 1),
                new MazePoint(-1, 0),
                new MazePoint(1, 0)
            };

            foreach(var dir in dirs) {
                MazePoint next = new MazePoint(current.X + dir.X, current.Y + dir.Y);

                if (next.X >= 0 && next.X < gridSize && next.Y >= 0 && next.Y < gridSize && maze[next.X, next.Y] == 0) {
                    int tentativeGScore = _gScore[current] + 1;

                    if (!_gScore.ContainsKey(next) || tentativeGScore < _gScore[next]) {
                        CameFrom[next] = current;
                        _gScore[next] = tentativeGScore;
                        int fScore = tentativeGScore + GetHeuristic(next);

                        _pq.Enqueue(next, fScore);
                        onNeighborAdded?.Invoke(next);
                    }
                }
            }
            return current;
        }
    }
}