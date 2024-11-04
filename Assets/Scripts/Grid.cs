using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public State this[int row, int col] {
        get => data[row, col];
        set => data[row, col] = value;
    }

    public Texture2D ToTexture2D(int textureWidth, int textureHeight) {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.filterMode = FilterMode.Point; // Garde la texture nette sans flou

        // Calcul des tailles des cellules dans la texture
        float cellWidth = (float)textureWidth / width;
        float cellHeight = (float)textureHeight / height;

        // Remplissage de la texture
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Color color = data[x, y] == State.ALIVE ? Color.white : Color.black;

                // Remplit les pixels correspondant à une cellule
                int pixelStartX = Mathf.RoundToInt(x * cellWidth);
                int pixelStartY = Mathf.RoundToInt(y * cellHeight);
                int pixelEndX = Mathf.RoundToInt((x + 1) * cellWidth);
                int pixelEndY = Mathf.RoundToInt((y + 1) * cellHeight);

                // Parcours de la zone de pixels pour appliquer la couleur
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
}

public enum State {
    ALIVE, DEAD
}
