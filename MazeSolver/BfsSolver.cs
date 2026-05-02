using System;
using System.Collections.Generic;

namespace MazeSolver {
    public class BfsSolver {
        private Queue < MazePoint > _queue;
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

        public BfsSolver(MazePoint start) {
            _queue = new Queue < MazePoint > ();
            _queue.Enqueue(start);

            Visited = new HashSet < MazePoint > {
                start
            };
            CameFrom = new Dictionary < MazePoint, MazePoint > ();
            IsDone = false;
            Steps = 0;
        }

        public MazePoint ? Step(int[, ] maze, int gridSize, Action < MazePoint > onNeighborAdded) {
            if (_queue.Count == 0) {
                IsDone = true;
                return null; // Exhausted
            }

            MazePoint current = _queue.Dequeue();
            Steps++;

            if (current.X == gridSize - 1 && current.Y == gridSize - 1) {
                IsDone = true;
                return current; // Reached Target
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
                        _queue.Enqueue(neighbor);

                        // Tell the UI to color this new neighbor
                        onNeighborAdded?.Invoke(neighbor);
                    }
                }
            }

            return current;
        }
    }
}