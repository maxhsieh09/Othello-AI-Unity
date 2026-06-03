using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class Visualize : MonoBehaviour
{
    public GameObject selectionPrefab;
    public GameObject piecePrefab;
    public List<GameObject> pieces = new List<GameObject>();
    public float spacing = 0.04f;
    public TextMeshProUGUI scoreText;
    public float AIDelay = 0.5f;

    Othello board = new();
    public int humanColor = 1; // 1 = Black, -1 = White
    int currentColor = 1;  // Tracks whose turn it actively is
    OthelloAI AI = new();
    bool isProcessingTurn = false;
    bool playing = true;

    void Start()
    {
        board.Reset();
        currentColor = 1; // Black always starts in Othello
        VisualizeBoard();
        
        // If human chose White, kick off the AI's first move immediately
        if (humanColor == -1)
        {
            _ = ProcessGameTurn();
        }
    }

    void Update()
    {
        if (!playing || isProcessingTurn) return;

        // Only capture human clicks if it is genuinely the human's turn
        if (currentColor == humanColor && Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var cellInfo = hit.transform.GetComponent<CellInfo>();
                if (cellInfo != null)
                {
                    HumanMove(cellInfo.x, cellInfo.y);
                }
            }
        }
    }

    void HumanMove(int x, int y)
    {
        ulong humanMoves = board.GetValidMoves(humanColor);
        if (Othello.GetFromBitmap(humanMoves, x, y) == 1)
        {
            board.MakeMove(x, y, humanColor);
            
            // Pass the turn to the AI side
            currentColor = -humanColor;
            VisualizeBoard();
            
            // Trigger turn processor
            _ = ProcessGameTurn();
        }
    }

    async Task ProcessGameTurn()
    {
        if (isProcessingTurn || !playing) return;
        isProcessingTurn = true;

        // 1. Check if the game is totally over
        if (board.IsFinished())
        {
            playing = false;
            isProcessingTurn = false;
            VisualizeBoard();
            return;
        }

        // 2. If it's the AI's turn to play
        if (currentColor == -humanColor)
        {
            // Verify if AI actually has moves to make
            if (board.GetValidMoves(currentColor) != 0)
            {
                await Task.Delay((int)(AIDelay * 1000));

                // Clear structural caching state variables before thinking to avoid stale reads
                int aiMove = await Task.Run(() => AI.GetBestMove(board, 4, currentColor));
                
                if (aiMove != -1)
                {
                    board.MakeMove(aiMove, currentColor);
                }
            }
            else
            {
                Debug.Log("AI has no moves! PASSING turn back to Human.");
            }

            // Move turn back to Human
            currentColor = humanColor;
            VisualizeBoard();

            // Fallback: If human is blocked immediately after AI turn, pass back to AI
            if (board.GetValidMoves(humanColor) == 0 && !board.IsFinished())
            {
                Debug.Log("Human has no moves! PASSING turn back to AI.");
                currentColor = -humanColor;
                isProcessingTurn = false;
                _ = ProcessGameTurn(); // Run turn processor again for AI
                return;
            }
        }

        isProcessingTurn = false;
    }

    void VisualizeBoard()
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

                    // Mark valid moves
                    if (Othello.GetFromBitmap(board.GetValidMoves(currentColor), x, y) == 1)
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

        UpdateScore();
    }

    void UpdateScore()
    {
        string text = $"Black: {board.GetScore(1)} / White: {board.GetScore(-1)}";
        if (board.IsFinished())
        {
            if (board.GetWinner() == 1)
            {
                text += " - Black wins!";
            } else if (board.GetWinner() == -1)
            {
                text += " - White wins!";
            } else
            {
                text += " - Draw!";
            }
        }
        scoreText.text = text;
    }
}
