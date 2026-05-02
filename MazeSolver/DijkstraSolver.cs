using System;
using System.Collections.Generic;

namespace MazeSolver {
    public class DijkstraSolver: IMazeSolver {
        private PriorityQueue < MazePoint, int > _pq;
        private Dictionary < MazePoint, int > _costSoFar;
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

        public DijkstraSolver(MazePoint start) {
            _pq = new PriorityQueue < MazePoint, int > ();
            _pq.Enqueue(start, 0);

            _costSoFar = new Dictionary < MazePoint, int > {
                [start] = 0
            };
            CameFrom = new Dictionary < MazePoint, MazePoint > ();
            IsDone = false;
            Steps = 0;
        }

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
                    int newCost = _costSoFar[current] + 1; // 1 step cost

                    if (!_costSoFar.ContainsKey(next) || newCost < _costSoFar[next]) {
                        _costSoFar[next] = newCost;
                        CameFrom[next] = current;
                        _pq.Enqueue(next, newCost);
                        onNeighborAdded?.Invoke(next);
                    }
                }
            }
            return current;
        }
    }
}