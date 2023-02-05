using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BoardDrawer : MonoBehaviour
{
    public Sprite tile;
    public Color[] tileColors = new Color[2];
    public Sprite[] pieceTypes;
    SpriteRenderer[] pieces;
    public GameObject piecePrefab;
    SpriteRenderer[] backgroundTiles;

    [Range(0, 20)]
    public int width = 3;

    private void Awake()
    {
        // Instantiate all of the possible piece locations and background tiles
        pieces = new SpriteRenderer[width * width];
        for (int i = 0; i < pieces.Length; i++)
        {
            GameObject tmpObj = Instantiate(piecePrefab, transform);
            tmpObj.AddComponent<SpriteRenderer>();
            pieces[i] = tmpObj.GetComponent<SpriteRenderer>();
            pieces[i].sprite = pieceTypes[0];
        }
        backgroundTiles = new SpriteRenderer[width * width];
        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pieces[x+  width * (2-y)].transform.position = new Vector3(x, y, 0);
                GameObject t = new GameObject("tile");
                t.transform.position = new Vector3(x, y, 0);
                t.AddComponent<SpriteRenderer>();
                t.GetComponent<SpriteRenderer>().sprite = tile;
                t.GetComponent<SpriteRenderer>().color = tileColors[((x + width * y) % 2 == 0) ? 0 : 1];
                //t.GetComponent<SpriteRenderer>().color = Color.green*(1f-((x + width * (2-y))/9f));
                backgroundTiles[x + width * (2-y)] = t.GetComponent<SpriteRenderer>();
            }
        }

    }

    void ResetColors()
    {
        for (int i = 0; i < width * width; i++)
            backgroundTiles[i].color = tileColors[(i % 2 == 0) ? 0 : 1];
    }


    public void HighlightWin(double[] gameBoard)
    {
        for (int team = 0; team < 2; team++)
        {
            double piece = -1;

            if (team == 1)
                piece = 1;

            if (gameBoard[0] == piece && gameBoard[3] == piece && gameBoard[6] == piece)
            {
                backgroundTiles[0].color = tileColors[2];
                backgroundTiles[3].color = tileColors[2];
                backgroundTiles[6].color = tileColors[2];
            }
            if (gameBoard[1] == piece && gameBoard[4] == piece && gameBoard[7] == piece)
            {
                backgroundTiles[1].color = tileColors[2];
                backgroundTiles[4].color = tileColors[2];
                backgroundTiles[7].color = tileColors[2];
            }
            if (gameBoard[2] == piece && gameBoard[5] == piece && gameBoard[8] == piece)
            {
                backgroundTiles[2].color = tileColors[2];
                backgroundTiles[5].color = tileColors[2];
                backgroundTiles[8].color = tileColors[2];
            }
            if (gameBoard[0] == piece && gameBoard[1] == piece && gameBoard[2] == piece)
            {
                backgroundTiles[0].color = tileColors[2];
                backgroundTiles[1].color = tileColors[2];
                backgroundTiles[2].color = tileColors[2];
            }
            if (gameBoard[3] == piece && gameBoard[4] == piece && gameBoard[5] == piece)
            {
                backgroundTiles[3].color = tileColors[2];
                backgroundTiles[4].color = tileColors[2];
                backgroundTiles[5].color = tileColors[2];
            }
            if (gameBoard[6] == piece && gameBoard[7] == piece && gameBoard[8] == piece)
            {
                backgroundTiles[6].color = tileColors[2];
                backgroundTiles[7].color = tileColors[2];
                backgroundTiles[8].color = tileColors[2];
            }
            if (gameBoard[0] == piece && gameBoard[4] == piece && gameBoard[8] == piece)
            {
                backgroundTiles[0].color = tileColors[2];
                backgroundTiles[4].color = tileColors[2];
                backgroundTiles[8].color = tileColors[2];
            }
            if (gameBoard[2] == piece && gameBoard[4] == piece && gameBoard[6] == piece)
            {
                backgroundTiles[2].color = tileColors[2];
                backgroundTiles[4].color = tileColors[2];
                backgroundTiles[6].color = tileColors[2];
            }
        }
    }

    public void Draw(double[] board)
    {
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
                pieces[i].sprite = pieceTypes[0];
            else if (board[i] == 1)
                pieces[i].sprite = pieceTypes[2];
            else if (board[i] == -1)
                pieces[i].sprite = pieceTypes[1];
        }
        HighlightWin(board);
    }

    public void Reset()
    {
        for (int i = 0; i < pieces.Length; i++)
            pieces[i].sprite = pieceTypes[0];

        ResetColors();
    }
}
