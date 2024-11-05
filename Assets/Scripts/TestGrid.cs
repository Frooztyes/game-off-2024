using Assets.Scripts.Chunks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets.Scripts {
    public class TestGrid : MonoBehaviour {
        [Header("Grid")]
        [SerializeField] int _gridWidth;
        [SerializeField] int _gridHeight;

        [Header("Display")]
        [SerializeField] int _width;
        [SerializeField] int _height;
        [SerializeField] Image _image;

        [Header("Automata")]
        [SerializeField] int _birthThreshold = 3;
        [SerializeField] int _survivalThreshold = 4;
        [SerializeField] int _iterations;
        [SerializeField] float _initialProba = 0.5f;
        [SerializeField, Range(0, 99999999)] int _seed = 0;
        [SerializeField] int _sizeFloodX = 1;
        [SerializeField] int _sizeFloodY = 1;

        [Header("Automata/Prefabs")]
        [SerializeField] ChunkMesh _prefabChunk;
        [SerializeField] int _chunkSize;
        [SerializeField] int _cellSize;

        [Header("Automata/Flood")]
        [SerializeField] int _areaSize = 40;

        [Header("Automata/Final")]
        [SerializeField] bool _cleanup;
        [SerializeField] bool _generateChunks;

        private Grid _grid;

        private void Start() {
            Random.InitState(_seed);
            _grid = new Grid(_gridWidth, _gridHeight, _initialProba);
            var rect = _image.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(_width, _height);
            Iterate();
        }

        void Iterate() {
            for (int i = 0; i < _iterations; i++) {
                _grid.AutomataIteration(_birthThreshold, _survivalThreshold);
            }

            _grid.FloodEverything(_areaSize);

            //_grid.AutomataIteration(_birthThreshold, _survivalThreshold);
            //_grid.AutomataIteration(_birthThreshold, _survivalThreshold);

            if(_cleanup) _grid.DeadOrAlive();

            _image.sprite = _grid.ToSprite(_width, _height);

            if (!_generateChunks) return;

            var chunks = _grid.SplitInChunks(_chunkSize);

            for (int i = 0; i < chunks.GetLength(0); i++) {
                for (int j = 0; j < chunks.GetLength(1); j++) {
                    var (data, position) = chunks[i, j];
                    var go = Instantiate(_prefabChunk, transform);
                    go.transform.position = position * (_chunkSize * _cellSize);
                    var borderedData = GetBorderedData(chunks, i, j);
                    go.Init(data, borderedData, _chunkSize, _cellSize);
                }
            }
        }

        private State[,] GetBorderedData((State[,] data, Vector3 position)[,] chunks, int i, int j) {
            State[,] borderedData = new State[_chunkSize + 2, _chunkSize + 2];

            for (int x = 0; x < _chunkSize + 2; x++) {
                for (int y = 0; y < _chunkSize + 2; y++) {
                    borderedData[x, y] = State.DEAD;

                    if (x > 0 && x <= _chunkSize && y > 0 && y <= _chunkSize) {
                        borderedData[x, y] = chunks[i, j].data[x - 1, y - 1];
                    }
                }
            }


            if (!IsOffGrid(i - 1, j, chunks.GetLength(0), chunks.GetLength(1))) {
                for (int y = 0; y < _chunkSize; y++) {
                    borderedData[0, y + 1] = chunks[i - 1, j].data[_chunkSize - 1, y];
                }
            }

            if (!IsOffGrid(i + 1, j, chunks.GetLength(0), chunks.GetLength(1))) {
                for (int y = 0; y < _chunkSize; y++) {
                    borderedData[_chunkSize + 1, y + 1] = chunks[i + 1, j].data[0, y];
                }
            }

            if (!IsOffGrid(i, j - 1, chunks.GetLength(0), chunks.GetLength(1))) {
                for (int x = 0; x < _chunkSize; x++) {
                    borderedData[x + 1, 0] = chunks[i, j - 1].data[x, _chunkSize - 1];
                }
            }

            if (!IsOffGrid(i, j + 1, chunks.GetLength(0), chunks.GetLength(1))) {
                for (int x = 0; x < _chunkSize; x++) {
                    borderedData[x + 1, _chunkSize + 1] = chunks[i, j + 1].data[x, 0];
                }
            }

            return borderedData;
        }

        private bool IsOffGrid(int x, int y, int xBound, int yBound) => x < 0 || y < 0 || x >= xBound || y >= yBound;
    }
}
