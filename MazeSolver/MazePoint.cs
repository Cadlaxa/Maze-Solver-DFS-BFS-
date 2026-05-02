using System;

namespace MazeSolver {
    public struct MazePoint {
        public int X {
            get;
        }
        public int Y {
            get;
        }

        public MazePoint(int x, int y) {
            X = x;
            Y = y;
        }

        public override bool Equals(object ? obj) => obj is MazePoint p && p.X == X && p.Y == Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}