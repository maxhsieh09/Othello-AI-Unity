using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class Directions
{
    const ulong notAFile = 0xFEFEFEFEFEFEFEFE;
    const ulong notHFile = 0x7F7F7F7F7F7F7F7F;

    // Pure Vertical (No file masking needed)
    public static ulong N(ulong x) => x << 8;
    public static ulong S(ulong x) => x >> 8;

    // Pure Horizontal
    public static ulong E(ulong x) => (x & notHFile) << 1;
    public static ulong W(ulong x) => (x & notAFile) >> 1;

    // Diagonals (Corrected Masks and Shifts)
    // NE = North (<< 8) + East (<< 1) = << 9. Must mask H-File before moving East.
    public static ulong NE(ulong x) => (x & notHFile) << 9;

    // NW = North (<< 8) + West (>> 1) = << 7. Must mask A-File before moving West.
    public static ulong NW(ulong x) => (x & notAFile) << 7;

    // SE = South (>> 8) + East (<< 1) = >> 7. Must mask H-File before moving East.
    public static ulong SE(ulong x) => (x & notHFile) >> 7;

    // SW = South (>> 8) + West (>> 1) = >> 9. Must mask A-File before moving West.
    public static ulong SW(ulong x) => (x & notAFile) >> 9;
}

public struct Othello
{
    public ulong black;
    public ulong white;

    public readonly ulong Empty => ~(black | white);
    ulong blackMovesCache;
    ulong whiteMovesCache;
    bool cached;

    public static int ToIndex(int x, int y)
    {
        return y * 8 + x;
    }

    public static int GetFromBitmap(ulong bitmap, int x, int y)
    {
        var index = ToIndex(x, y);
        return (bitmap & (1UL << index)) != 0 ? 1 : 0;
    }

    public int GetPiece(int x, int y)
    {
        return GetFromBitmap(black, x, y) - GetFromBitmap(white, x, y);
    }

    public static int CountBits(ulong value)
    {
        int count = 0;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }

    public void Reset()
    {
        black = 1UL << ToIndex(3, 3);
        black |= 1UL << ToIndex(4, 4);
        white = 1UL << ToIndex(3, 4);
        white |= 1UL << ToIndex(4, 3);

        cached = false;
        blackMovesCache = GetValidMoves(1);
        whiteMovesCache = GetValidMoves(-1);
        cached = true;
    }

    public bool IsFinished()
    {
        return GetValidMoves(1) == 0 && GetValidMoves(-1) == 0;
    }

    public int GetScore(int color)
    {
        return CountBits(color == 1 ? black : white);
    }

    public int GetWinner()
    {
        int diff = GetScore(1) - GetScore(-1);
        return diff > 0 ? 1 : diff < 0 ? -1 : 0;
    }

    public ulong GetValidMoves(int color)
    {
        if (cached)
        {
            return color == 1 ? blackMovesCache : whiteMovesCache;
        }

        ulong friendly = color == 1 ? black : white;
        ulong opponent = color == 1 ? white : black;
        ulong moves = 0;

        moves |= ValidDirectionMoves(friendly, opponent, Directions.N);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.S);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.E);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.W);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.NE);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.NW);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.SE);
        moves |= ValidDirectionMoves(friendly, opponent, Directions.SW);

        if (color == 1)
        {
            blackMovesCache = moves;
        } else
        {
            whiteMovesCache = moves;
        }
        return moves;
    }

    ulong ValidDirectionMoves(ulong friendly, ulong opponent, Func<ulong, ulong> shiftFunc)
    {
        ulong candidates = shiftFunc(friendly) & opponent;
        for (int i = 0; i < 5; i++)
        {
            candidates |= shiftFunc(candidates) & opponent;
        }
        return shiftFunc(candidates) & Empty;
    }

    public void MakeMove(int x, int y, int color)
    {
        MakeMove(ToIndex(x, y), color);
    }

    public void MakeMove(int index, int color)
    {
        if (color == 1)
        {
            MakeMove(ref black, ref white, 1UL << index);
        } else
        {
            MakeMove(ref white, ref black, 1UL << index);
        }
    }

    void MakeMove(ref ulong friendly, ref ulong opponent, ulong moveBit)
    {
        ulong flipped = 0;

        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.N);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.S);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.E);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.W);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.NE);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.NW);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.SE);
        flipped |= FlipDirection(friendly, opponent, moveBit, Directions.SW);

        friendly |= moveBit | flipped;
        opponent &= ~flipped;
        Debug.Assert((friendly & opponent) == 0);

        cached = false;
        blackMovesCache = GetValidMoves(1);
        whiteMovesCache = GetValidMoves(-1);
        cached = true;
    }

    ulong FlipDirection(ulong friendly, ulong opponent, ulong moveBit, Func<ulong, ulong> shiftFunc)
    {
        ulong continuousOpponent = 0;
        ulong current = shiftFunc(moveBit);
        while ((current & opponent) != 0)
        {
            continuousOpponent |= current;
            current = shiftFunc(current);
        }

        if ((current & friendly) != 0)
        {
            return continuousOpponent;
        }
        return 0;
    }

    public void Print()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var piece = GetPiece(x, y);
                if (piece == 1)
                {
                    Debug.Log("⚫️");
                }
                else if (piece == -1)
                {
                    Debug.Log("⚪️");
                }
                else
                {
                    Debug.Log("+");
                }
            }
        }
    }
}
