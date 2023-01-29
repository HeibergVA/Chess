using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece // Laver en pawn klasse baseret p� chesspiece klassen
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1; //op hvis vi er hvid ned hvis vi er sort

        //En frem
        if (board[currentX,currentY+direction]==null)
            r.Add(new Vector2Int(currentX,currentY+direction));

        //2 frem tr�k
        if (board[currentX, currentY + direction] == null)
        {
            //Hvid hold
            if(team==0 && currentY==1 && board[currentX, currentY+(direction*2)]==null)
                r.Add(new Vector2Int(currentX,currentY+(direction*2)));
            //Sort hold
            if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
        }
        //Tag skr�t

        if (currentX != tileCountX - 1)
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY + direction));

        return r;
    }
    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;
        //Promotion
        if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
            return SpecialMove.Promotion;

        //En Passant
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (board[lastMove[1].x,lastMove[1].y].type == ChessPieceType.Pawn) //er det sidste tr�k lavet af en bonde?
            {
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y)== 2) //Hvis den rykkede 2 frem
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team) // Hvis tr�kket er fra det andet hold.
                    {
                        if (lastMove[1].y == currentY) // Hvis begge bonder er p� samme y akse
                        {
                            if (lastMove[1].x == currentX - 1) // Til venstre 
                            {
                                availableMoves.Add(new Vector2Int(currentX-1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                            if (lastMove[1].x == currentX + 1) // Til h�jre
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;

                            }
                        }
                    }
                }
            }
        }

        return SpecialMove.None;
    }
}
