using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OthelloAI
{
    const ulong cornerMask = 0x8100000000000081;
    const ulong edgeMask = 0x7e8181818181817e;
    const ulong dangerMask = 0x42000000004200;

    float Evaluate(Othello board)
    {
        if (board.IsFinished())
        {
            return board.GetWinner() * 2000 + board.GetScore(1) - board.GetScore(-1);
        }

        return EvaluateSingle(board, 1) - EvaluateSingle(board, -1);
    }

    float EvaluateSingle(Othello board, int color)
    {
        ulong bitmap = board.black;
        if (color == -1) bitmap = board.white;
        
        int total = Othello.CountBits(bitmap);
        int corners = Othello.CountBits(bitmap & cornerMask);
        int edges = Othello.CountBits(bitmap & edgeMask);
        int danger = Othello.CountBits(bitmap & dangerMask);
        int mobility = Othello.CountBits(board.GetValidMoves(color));

        return corners * 20 + edges * 2 - danger * 5 + total + mobility * 3;
    }

    IEnumerable<int> GenerateMoves(ulong bitmap)
    {
        // Corners
        foreach (var move in new int[] { 0, 7, 56, 63 })
        {
            if ((bitmap & (1UL << move)) != 0)
            {
                yield return move;
            }
        }

        // Edges
        for (int i = 1; i < 7; i++)
        {
            foreach (var move in new int[] { i, i + 56, i * 8, i * 8 + 7 })
            {
                if ((bitmap & (1UL << move)) != 0)
                {
                    yield return move;
                }
            }
        }

        // Others
        for (int i = 1; i < 7; i++)
        {
            for (int j = 1; j < 7; j++)
            {
                var move = i * 8 + j;
                if ((bitmap & (1UL << move)) != 0)
                {
                    yield return move;
                }
            }
        }
    }

    float Negamax(Othello board, int depth, float alpha, float beta, int color)
    {
        if (depth == 0 || board.IsFinished())
        {
            return Evaluate(board) * color;
        }

        if (board.GetValidMoves(color) == 0)
        {
            return -Negamax(board, depth - 1, -beta, -alpha, -color);
        }

        float value = float.MinValue;

        foreach (var move in GenerateMoves(board.GetValidMoves(color)))
        {
            var boardCopy = board; // Othello is struct, so we can make deep copy with assignment
            boardCopy.MakeMove(move, color);
            value = Mathf.Max(value, -Negamax(boardCopy, depth - 1, -beta, -alpha, -color));
            alpha = Mathf.Max(alpha, value);
            if (alpha >= beta)
            {
                break;
            }
        }

        return value;
    }

    public int GetBestMove(Othello board, int depth, int color)
    {
        var sysRandom = new System.Random();

        float bestValue = float.MinValue;
        int bestMove = -1;
        foreach (var move in GenerateMoves(board.GetValidMoves(color)))
        {
            var boardCopy = board;
            boardCopy.MakeMove(move, color);
            float value = -Negamax(boardCopy, depth, float.MinValue, float.MaxValue, -color);
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = move;
            }
            else if (value == bestValue && sysRandom.NextDouble() < 0.5)
            {
                bestMove = move; // Break ties randomly
            }
        }

        if (bestMove == -1)
        {
            Debug.LogWarning($"No valid moves for AI, {board.GetValidMoves(color)}");
        }
        Debug.Log($"Value: {bestValue}");
        return bestMove;
    }
}
