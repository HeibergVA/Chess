using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece // Laver en Rook klasse baseret p� chesspiece klassen
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();  //Laver en liste af vektor

        //Ryk ned
        for (int i = currentY - 1; i >= 0; i--)   //Laver et for-loop der k�re en gang for hvert felt under der hvor brikken st�r nu
        {
            if (board[currentX, i]==null)    //Hvis felttet er tomt, alts� lig med nul
                r.Add(new Vector2Int(currentX, i));    //Tilf�jer dette felt til listen over lovlige tr�k
            if (board[currentX, i] != null)     //Hvis felttet ikke er lig med nul, alts� at der st�r en brik p� felttet
            {
                if(board[currentX, i].team !=team)      //Hvis den brik der st�r p� feltet ikke er p� dit eget hold
                    r.Add(new Vector2Int(currentX, i));   //Tilf�jer dette felt til listen over lovlige tr�k

                break;  //G�r ud af loopet
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
        //Ryk h�jre
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
