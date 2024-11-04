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

        [Header("Automata/Flood")]
        [SerializeField] int _areaSize = 40;

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

            _grid.AutomataIteration(_birthThreshold, _survivalThreshold);
            _grid.AutomataIteration(_birthThreshold, _survivalThreshold);

            _image.sprite = _grid.ToSprite(_width, _height);
        }
    }
}
