using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public enum ChessPieceType //Definerer vores skakbrik typer
{
    None=0,
    Pawn=1,
    Rook=2,
    Knight=3,
    Bishop=4,
    Queen=5,
    King=6
}
public class ChessPiece : MonoBehaviour //Klasse til skakbrikker.
{
    public int team; //hvilket hold brikken er 0=hvid og 1=sort 
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = new Vector3(0.00035f, 0.00035f, 0.00035f);

    private void Start()
    {
        transform.rotation = Quaternion.Euler((team ==1) ? Vector3.zero : new Vector3(0, 180, 0));
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);

    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY) // Mulige tr�k
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));

        return r;

    }
    public virtual SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        return SpecialMove.None;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }


}
