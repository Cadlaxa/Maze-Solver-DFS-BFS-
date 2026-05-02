using System;
using System.Collections.Generic;

namespace MazeSolver {
    public interface IMazeSolver {
        bool IsDone {get;}
        int Steps {get;}
        Dictionary < MazePoint, MazePoint > CameFrom {get;}
        MazePoint ? Step(int[, ] maze, int gridSize, Action < MazePoint > onNeighborAdded);
    }
}