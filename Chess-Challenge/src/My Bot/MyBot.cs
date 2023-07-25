using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    public class MyBot : IChessBot
    {
        //                     .  P    K    B    R    Q    K
        int[] kPieceValues = { 0, 100, 310, 315, 500, 900, 500 };
        int kMassiveNum = 99999999;

        int mDepth;
        Move mBestMove;

        public Move Think(Board board, Timer timer)
        {
            Move[] legalMoves = board.GetLegalMoves();
            mDepth = 3;

            EvaluateBoardNegaMax(board, mDepth, -kMassiveNum, kMassiveNum, board.IsWhiteToMove ? 1 : -1);

            return mBestMove;
        }

        int EvalPositional(ulong bb, Board board, bool side, int piece)
        {
            int eval = 25 * BitboardHelper.GetNumberOfSetBits(bb);
            for (int p = 1; ++p < 7;)
            {
                eval += BitboardHelper.GetNumberOfSetBits(bb & board.GetPieceBitboard((PieceType)p, !side)) * kPieceValues[p];
            }
            for (int p = 1; ++p < 6;)
            {
                eval += BitboardHelper.GetNumberOfSetBits(bb & board.GetPieceBitboard((PieceType)p, side)) * kPieceValues[p];
            }
            return eval / 15;
        }
        int EvaluateBoardNegaMax(Board board, int depth, int alpha, int beta, int color)
        {
            Move[] legalMoves;

            if (board.IsDraw())
                return 0;

            if (board.IsInCheckmate())
                return -999999999;

            if (depth == 0 || (legalMoves = board.GetLegalMoves()).Length == 0)
            {
                // EVALUATE
                int sum = 0;


                // material
                for (int i = 0; ++i < 7;)
                    sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * kPieceValues[i];

                foreach (bool side in new[] { true, false })
                {
                    int sign = side ? 1 : -1;
                    foreach (Piece p in board.GetPieceList((PieceType)1, side))
                    {
                        sum += sign * EvalPositional(BitboardHelper.GetPawnAttacks(p.Square, side), board, side, 1);
                    }
                    foreach (Piece p in board.GetPieceList((PieceType)2, side))
                    {
                        sum += sign * EvalPositional(BitboardHelper.GetKnightAttacks(p.Square), board, side, 2);
                    }
                    foreach (Piece p in board.GetPieceList((PieceType)6, side))
                    {
                        sum += sign * EvalPositional(BitboardHelper.GetKingAttacks(p.Square), board, side, 6);
                    }
                    for (int t = 3; ++t < 6;)
                    {
                        foreach (Piece p in board.GetPieceList((PieceType)t, side))
                        {
                            sum += sign * EvalPositional(BitboardHelper.GetSliderAttacks((PieceType)t, p.Square, board), board, side, t);
                        }
                    }

                    Square sqKing = board.GetKingSquare(side);
                    sum += (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) - 16) * (int)(((float)sqKing.File - 3.5) * ((float)sqKing.File - 3.5) + ((float)sqKing.Rank - 3.5) * ((float)sqKing.Rank - 3.5)) / 10;
                }


                return color * sum;
            }

            // TREE SEARCH
            int recordEval = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int evaluation = -EvaluateBoardNegaMax(board, depth - 1, -beta, -alpha, -color);
                board.UndoMove(move);

                if (recordEval < evaluation)
                {
                    recordEval = evaluation;
                    if (depth == mDepth)
                        mBestMove = move;
                }
                alpha = Math.Max(alpha, recordEval);
                if (alpha >= beta) break;
            }
            // TREE SEARCH

            return recordEval;
        }
    }
}