using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualize : MonoBehaviour
{
    public GameObject selectionPrefab;
    public GameObject piecePrefab;
    public List<GameObject> pieces = new List<GameObject>();
    public float spacing = 0.04f;

    Othello board = new();
    int color = 1;
    ulong validMovesCache = 0;

    // Start is called before the first frame update
    void Start()
    {
        board.Reset();
        validMovesCache = board.GetValidMoves(color);
        UpdateBoard();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var cellInfo = hit.transform.GetComponent<CellInfo>();
                
                // If we clicked on a valid move
                if (cellInfo != null && Othello.GetFromBitmap(validMovesCache, cellInfo.x, cellInfo.y) == 1)
                {
                    board.MakeMove(cellInfo.x, cellInfo.y, color);
                    color = -color;
                    validMovesCache = board.GetValidMoves(color);
                    if (validMovesCache == 0)
                    {
                        color = -color;
                        validMovesCache = board.GetValidMoves(color);
                    }
                    UpdateBoard();
                }
            }
        }
    }

    void UpdateBoard()
    {
        foreach (var piece in pieces)
        {
            Destroy(piece);
        }
        pieces.Clear();
        
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int value = board.GetPiece(x, y);
                Vector3 piecePosition = transform.position + new Vector3((x - 3.5f) * spacing, 0, (y - 3.5f) * spacing);

                if (value == 0)
                {
                    var selection = Instantiate(selectionPrefab, piecePosition, Quaternion.identity, transform);
                    var cellInfo = selection.GetComponent<CellInfo>();
                    cellInfo.x = x;
                    cellInfo.y = y;
                    if (Othello.GetFromBitmap(validMovesCache, x, y) == 1)
                    {
                        selection.GetComponent<Renderer>().material.color = Color.red;
                    }
                    pieces.Add(selection);
                } else
                {
                    var piece = Instantiate(piecePrefab, piecePosition, Quaternion.identity, transform);
                    piece.GetComponent<Renderer>().material.color = value == 1 ? Color.black : Color.white;
                    pieces.Add(piece);
                }
            }
        }
    }
}
