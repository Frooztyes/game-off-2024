using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Chunks {
    public class ChunkMesh : MonoBehaviour {
        [SerializeField] private MeshFilter _wallMesh;
        [SerializeField] private MeshFilter _floorMesh;

        State[,] _data;
        State[,] _borderedData;
        int _chunkSize;
        int _cellSize;

        private bool IsOffGrid(int x, int y) => x < 0 || y < 0 || x >= _chunkSize || y >= _chunkSize;

        public void Init(State[,] data, State[,] borderedData, int chunkSize, int cellSize) {
            _data = data;
            _borderedData = borderedData;
            _chunkSize = chunkSize;
            _cellSize = cellSize;
            GenerateMesh();
        }

        private void GenerateMesh() {

            List<Vector3> wallVertices = new();
            List<int> wallTriangles = new();
            List<Vector2> wallUVs = new();

            List<Vector3> floorVertices = new();
            List<int> floorTriangles = new();
            List<Vector2> floorUVs = new();

            for (int i = 0; i < _chunkSize; i++) {
                for (int j = 0; j < _chunkSize; j++) {
                    if (_data[i, j] == State.ALIVE) {
                        AddFloor(floorTriangles, floorVertices, floorUVs, i, j);
                        AddWalls(wallTriangles, wallVertices, wallUVs, i, j);
                    }
                }
            }

            Mesh mesh = new() {
                vertices = wallVertices.ToArray(),
                triangles = wallTriangles.ToArray(),
                uv = wallUVs.ToArray(),
            };
            mesh.RecalculateNormals();
            _wallMesh.mesh = mesh;

            mesh = new() {
                vertices = floorVertices.ToArray(),
                triangles = floorTriangles.ToArray(),
                uv = floorUVs.ToArray(),
            };
            mesh.RecalculateNormals();
            _floorMesh.mesh = mesh;
        }

        private void AddFloor(List<int> triangles, List<Vector3> vertices, List<Vector2> uvs, int i, int j) {
            int baseIndex = vertices.Count;

            int iP = i + 1;
            int jp = j + 1;

            i *= _cellSize;
            j *= _cellSize;
            iP *= _cellSize;
            jp *= _cellSize;

            vertices.Add(new Vector3(i, 0, j));
            vertices.Add(new Vector3(iP, 0, j));
            vertices.Add(new Vector3(iP, 0, jp));
            vertices.Add(new Vector3(i, 0, jp));

            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex);

            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }


        private void OnDrawGizmosSelected() {
            if (!Application.isPlaying) return;
            for (int i = 0; i < _chunkSize + 2; i++) {
                for (int j = 0; j < _chunkSize + 2; j++) {
                    if (_borderedData[i, j] == State.ALIVE) {
                        Gizmos.color = Color.white;
                    }
                    else {
                        Gizmos.color = Color.black;
                    }

                    var position = new Vector3((i - 1) * _cellSize, 0, (j - 1) * _cellSize) + (new Vector3(1, 0, 1) * (_cellSize / 2));
                    position += transform.position;
                         
                    Gizmos.DrawSphere(position, 3);
                }
            }

            for (int i = 0; i < _chunkSize; i++) {
                for (int j = 0; j < _chunkSize; j++) {
                    if (_data[i, j] == State.ALIVE) {
                        Gizmos.color = Color.green;
                    }
                    else {
                        Gizmos.color = Color.red;
                    }

                    var position = new Vector3(i * _cellSize, 0, j * _cellSize) + (new Vector3(1, 0, 1) * (_cellSize / 2));
                    position += transform.position;

                    Gizmos.DrawSphere(position, 3);
                }
            }
        }

        private void AddWalls(List<int> triangles, List<Vector3> vertices, List<Vector2> uvs, int i, int j) {
            if (IsDead(i - 1, j)) {
                AddVerticalFace(triangles, vertices, uvs, i, j, Vector3.forward, false);
            }

            if (IsDead(i + 1, j)) {
                AddVerticalFace(triangles, vertices, uvs, i + 1, j, Vector3.forward, true);
            }


            if (IsDead(i, j - 1)) {
                AddVerticalFace(triangles, vertices, uvs, i, j, Vector3.right, true);
            }

            if (IsDead(i, j + 1)) {
                AddVerticalFace(triangles, vertices, uvs, i, j + 1, Vector3.right, false);
            }
        }

        private bool IsDead(int i, int j) {
            // check in the border
            if(IsOffGrid(i, j)) {
                return _borderedData[i + 1, j + 1] == State.DEAD;
            }

            return _data[i, j] == State.DEAD;
        }

        private void AddVerticalFace(List<int> triangles, List<Vector3> vertices, List<Vector2> uvs, int i, int j, Vector3 direction, bool reverse) {
            int baseIndex = vertices.Count;


            float iP = i + direction.x;
            float jp = j + direction.z;

            i *= _cellSize;
            j *= _cellSize;
            iP *= _cellSize;
            jp *= _cellSize;

            // Positions des sommets pour une face verticale
            Vector3 bottomLeft = new Vector3(i, 0, j);
            Vector3 bottomRight = new Vector3(iP, 0, jp);
            Vector3 topLeft = new Vector3(i, _cellSize, j);
            Vector3 topRight = new Vector3(iP, _cellSize, jp);

            // Ajouter les sommets
            vertices.Add(bottomLeft);
            vertices.Add(bottomRight);
            vertices.Add(topRight);
            vertices.Add(topLeft);

            // Ajouter les triangles
            triangles.Add(baseIndex + (reverse ? 0 : 2));
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + (reverse ? 2 : 0));

            triangles.Add(baseIndex + (reverse ? 0 : 3));
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + (reverse ? 3 : 0));

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1)); 
            uvs.Add(new Vector2(0, 1));
        }

    }
}
