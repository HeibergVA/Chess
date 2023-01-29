using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece // Laver en Rook klasse baseret på chesspiece klassen
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();  //Laver en liste af vektor

        //Ryk ned
        for (int i = currentY - 1; i >= 0; i--)   //Laver et for-loop der køre en gang for hvert felt under der hvor brikken står nu
        {
            if (board[currentX, i]==null)    //Hvis felttet er tomt, altså lig med nul
                r.Add(new Vector2Int(currentX, i));    //Tilføjer dette felt til listen over lovlige træk
            if (board[currentX, i] != null)     //Hvis felttet ikke er lig med nul, altså at der står en brik på felttet
            {
                if(board[currentX, i].team !=team)      //Hvis den brik der står på feltet ikke er på dit eget hold
                    r.Add(new Vector2Int(currentX, i));   //Tilføjer dette felt til listen over lovlige træk

                break;  //Gør ud af loopet
            }
                
        }

        //Ryk op
        for (int i = currentY + 1; i < tileCountY; i++)
        {
            if (board[currentX, i] == null)
                r.Add(new Vector2Int(currentX, i));
            if (board[currentX, i] != null)
            {
                if (board[currentX, i].team != team)
                    r.Add(new Vector2Int(currentX, i));

                break;

            }

        }

        //Ryk Venstre
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (board[i, currentY] == null)
                r.Add(new Vector2Int(i, currentY));
            if (board[i, currentY] != null)
            {
                if (board[i, currentY].team != team)
                    r.Add(new Vector2Int(i, currentY));

                break;

            }

        }
        //Ryk højre
        for (int i = currentX + 1; i < tileCountX; i++)
        {
            if (board[i, currentY] == null)
                r.Add(new Vector2Int(i, currentY));
            if (board[i, currentY] != null)
            {
                if (board[i, currentY].team != team)
                    r.Add(new Vector2Int(i, currentY));

                break;

            }

        }

        return r;
    }
}
