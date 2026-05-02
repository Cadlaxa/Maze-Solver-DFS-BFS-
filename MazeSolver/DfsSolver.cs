using System;
using System.Collections.Generic;

namespace MazeSolver {
    public class DfsSolver: IMazeSolver {
        private Stack < MazePoint > _stack;
        public HashSet < MazePoint > Visited {
            get;
        }
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

        public DfsSolver(MazePoint start) {
            _stack = new Stack < MazePoint > ();
            _stack.Push(start);

            Visited = new HashSet < MazePoint > {
                start
            };
            CameFrom = new Dictionary < MazePoint, MazePoint > ();
            IsDone = false;
            Steps = 0;
        }

        public MazePoint ? Step(int[, ] maze, int gridSize, Action < MazePoint > onNeighborAdded) {
            if (_stack.Count == 0) {
                IsDone = true;
                return null;
            }

            MazePoint current = _stack.Pop();
            Steps++;

            if (current.X == gridSize - 1 && current.Y == gridSize - 1) {
                IsDone = true;
                return current;
            }

            MazePoint[] directions = {
                new MazePoint(0, -1),
                new MazePoint(0, 1),
                new MazePoint(-1, 0),
                new MazePoint(1, 0)
            };

            foreach(var dir in directions) {
                MazePoint neighbor = new MazePoint(current.X + dir.X, current.Y + dir.Y);

                if (neighbor.X >= 0 && neighbor.X < gridSize && neighbor.Y >= 0 && neighbor.Y < gridSize && maze[neighbor.X, neighbor.Y] == 0) {
                    if (!Visited.Contains(neighbor)) {
                        Visited.Add(neighbor);
                        CameFrom[neighbor] = current;
                        _stack.Push(neighbor);

                        onNeighborAdded?.Invoke(neighbor);
                    }
                }
            }

            return current;
        }
    }
}