using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using DungeonGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using Edge = DungeonGen.Edge;
using Point = DungeonGen.Point;
using Random = UnityEngine.Random;

public class Grid {
    State[,] data;
    int width;
    int height;

    public Grid(int width, int height, float aliveProbability) {
        data = new State[width, height];
        this.width = width;
        this.height = height;
        InitGrid(aliveProbability);
    }

    private void InitGrid(float aliveProbability) {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                data[i, j] = Random.value < aliveProbability ? State.ALIVE : State.DEAD;
            }
        }
    }

    private bool IsOffGrid(int x, int y) => x < 0 || y < 0 || x >= width || y >= height;

    private List<State> CheckNeighboor(int x, int y) {
        List<State> states = new();
        for (int i = x - 1; i < x + 2; i++) {
            for (int j = y - 1; j < y + 2; j++) {
                if (i == x && j == y) continue;

                if (!IsOffGrid(i, j)) {
                    states.Add(data[i, j]);
                }
            }
        }
        return states;
    }

    private State NewValueAtPosition(int x, int y, int birthThreshold, int survivalThreshold) {
        int aliveNeighboor = CheckNeighboor(x, y).Count((v) => v != State.DEAD);
        bool cellAlive = data[x, y] != State.DEAD;

        if (cellAlive && aliveNeighboor >= survivalThreshold) return State.ALIVE;
        if (!cellAlive && aliveNeighboor >= birthThreshold) return State.ALIVE;
        return State.DEAD;
    }

    public void AutomataIteration(int birthThreshold, int survivalThreshold) {
        State[,] newData = new State[width, height];
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                newData[i, j] = NewValueAtPosition(i, j, birthThreshold, survivalThreshold);
            }
        }
        data = newData;
    }

    public DelaunatorSharp.Point? FloodFill(int startX, int startY, int threshSize, int sizeX = 1, int sizeY = 1) {
        List<(int x, int y)> updated = new();

        Queue<(int x, int y)> queue = new();
        queue.Enqueue((startX, startY));

        int area = 0;

        while (queue.Count > 0) {
            var n = queue.Dequeue();
            if (IsOffGrid(n.x, n.y) || data[n.x, n.y] != State.ALIVE) continue;
            data[n.x, n.y] = State.VISITED;
            updated.Add(n);
            area++;

            for (int dx = -sizeX; dx <= sizeX; dx++) {
                for (int dy = -sizeY; dy <= sizeY; dy++) {
                    if (dx == 0 && dy == 0) continue; // Ignorer la cellule actuelle
                    queue.Enqueue((n.x + dx, n.y + dy));
                }
            }
        }

        if (area < threshSize) {
            foreach (var n in updated) {
                data[n.x, n.y] = State.DEAD;
            }
            return null;
        }
        else {
            var (x, y) = updated[Random.Range(0, updated.Count - 1)];
            return new DelaunatorSharp.Point(x, y);
        }
    }

    public void FloodEverything(int thresholdArea) {
        List<IPoint> points = new();

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (data[i, j] != State.ALIVE) continue;

                var pt = FloodFill(i, j, thresholdArea);
                if (pt.HasValue)
                    points.Add(pt.Value);
            }
        }

        Delaunization(points.ToArray());
    }

    public void Delaunization(IPoint[] points) {
        Delaunator delaunator = new(points);
        List<Edge> edges = new();
        
        delaunator.ForEachTriangleEdge(edge => {
            var start = Vector2Int.CeilToInt(edge.P.ToVector2());
            var end = Vector2Int.CeilToInt(edge.Q.ToVector2());

            Point startPt = new(start.x, start.y);
            Point endPt = new(end.x, end.y);
            edges.Add(new(startPt, endPt));
        });

        var mstEdges = Kruskal.MinimumSpanningTree(edges);

        foreach (var mstEdge in mstEdges) {
            CreateTunnel(mstEdge.a.x, mstEdge.a.y, mstEdge.b.x, mstEdge.b.y, 2);
            data[mstEdge.a.x, mstEdge.a.y] = State.TRUC;
            data[mstEdge.b.x, mstEdge.b.y] = State.TRUC;
        }

        var pts = FindDeadEnds(mstEdges);
        foreach (var pt in pts) {
            data[pt.x, pt.y] = State.DEAD_END;
        }
    }

    public static List<Point> FindDeadEnds(List<Edge> mstEdges) {
        Dictionary<Point, int> pointDegrees = new();

        foreach (var edge in mstEdges) {
            if (pointDegrees.ContainsKey(edge.a))
                pointDegrees[edge.a]++;
            else
                pointDegrees[edge.a] = 1;

            if (pointDegrees.ContainsKey(edge.b))
                pointDegrees[edge.b]++;
            else
                pointDegrees[edge.b] = 1;
        }

        List<Point> deadEnds = new();

        foreach (var point in pointDegrees) {
            if (point.Value == 1)
            {
                deadEnds.Add(point.Key);
            }
        }

        return deadEnds;
    }

    public void CreateTunnel(int x0, int y0, int x1, int y1, float wd) {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx - dy, e2, x2, y2;  // error value e_xy
        float ed = dx + dy == 0 ? 1 : (float)Math.Sqrt(dx * dx + dy * dy);

        wd = (wd + 1) / 2;  // adjust the width

        while (true) // pixel loop
        {
            data[x0, y0] = State.ALIVE;
            e2 = err;
            x2 = x0;

            if (2 * e2 >= -dx) // x step
            {
                for (e2 += dy, y2 = y0; e2 < ed * wd && (y1 != y2 || dx > dy); e2 += dx) {
                    data[x0, y2 += sy] = State.ALIVE;
                }
                if (x0 == x1) break;
                e2 = err;
                err -= dy;
                x0 += sx;
            }

            if (2 * e2 <= dy) // y step
            {
                for (e2 = dx - e2; e2 < ed * wd && (x1 != x2 || dx < dy); e2 += dy) {
                    data[x2 += sx, y0] = State.ALIVE;
                }
                if (y0 == y1) break;
                err += dx;
                y0 += sy;
            }
        }
    }

    public void DeadOrAlive() {
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                data[i, j] = data[i, j] == State.DEAD ? State.DEAD : State.ALIVE;
            }
        }
    }

    public (State[,] data, Vector3 position)[,] SplitInChunks(int chunkSize) {

        int nbChunksX = (width / chunkSize) + 1;
        int nbChunksY = (height / chunkSize) + 1;


        (State[,] data, Vector3 position)[,] chunks = new (State[,] data, Vector3 position)[nbChunksX, nbChunksY];

        for (int i = 0; i < nbChunksX; i++) {
            for (int j = 0; j < nbChunksY; j++) {
                State[,] currentChunk = new State[chunkSize, chunkSize];

                // parcours des valeurs du chunk
                for (int x = 0; x < chunkSize; x++) {
                    for (int y = 0; y < chunkSize; y++) {
                        // Calculer les indices dans `data`
                        int dataX = i * chunkSize + x;
                        int dataY = j * chunkSize + y;

                        // Vérifier que les indices sont dans les limites de `data`
                        if (dataX < width && dataY < height) {
                            currentChunk[x, y] = data[dataX, dataY];
                        }
                        else {
                            currentChunk[x, y] = State.DEAD; // Valeur par défaut si en dehors des limites
                        }
                    }
                }
                chunks[i, j] = (currentChunk, new Vector3(i, 0, j));
            }
        }
        return chunks;
    }

    public Texture2D ToTexture2D(int textureWidth, int textureHeight) {
        Texture2D texture = new(textureWidth, textureHeight) {
            filterMode = FilterMode.Point
        };

        float cellWidth = (float)textureWidth / width;
        float cellHeight = (float)textureHeight / height;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Color color = data[x, y] switch {
                    State.ALIVE => Color.white,
                    State.DEAD => Color.black,
                    State.VISITED => Color.yellow,
                    State.TRUC => Color.blue,
                    State.DEAD_END => Color.green,
                    _ => Color.magenta
                };

                int pixelStartX = Mathf.RoundToInt(x * cellWidth);
                int pixelStartY = Mathf.RoundToInt(y * cellHeight);
                int pixelEndX = Mathf.RoundToInt((x + 1) * cellWidth);
                int pixelEndY = Mathf.RoundToInt((y + 1) * cellHeight);

                for (int px = pixelStartX; px < pixelEndX; px++) {
                    for (int py = pixelStartY; py < pixelEndY; py++) {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        // Applique les modifications à la texture
        texture.Apply();

        return texture;
    }

    public Sprite ToSprite(int textureWidth, int textureHeight) {
        Texture2D texture = ToTexture2D(textureWidth, textureHeight);
        Rect rect = new(0, 0, textureWidth, textureHeight);
        Vector2 pivot = new(0.5f, 0.5f);

        return Sprite.Create(texture, rect, pivot);
    }

}

public enum State {
    ALIVE, DEAD, VISITED, TRUC, DEAD_END
}
