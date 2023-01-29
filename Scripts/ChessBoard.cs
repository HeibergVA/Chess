using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public enum SpecialMove
{
    None=0,
    EnPassant,
    Castling,
    Promotion,
}
public class ChessBoard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.00015f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 0.7f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Transform rematchIndicator;


    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    // Logic 
    private ChessPiece[,] chessPieces; //Et array af vores brikker
    private ChessPiece currentlyDragging; //Trækker en brik
    private List<ChessPiece> deadWhites = new List<ChessPiece> ();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int[]> moveList = new List<Vector2Int[]> (); // Liste af træk
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles; // Hvert tile er et game object. I et array af tiles
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds; // Hvor langt væk er slutningen af skakbrættet
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private SpecialMove specialMove;
    private bool isWhiteTurn;

    //multilogic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    private void Start() // Kode der skal køres i starten af programmet
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();

        PositionAllPieces();

        RegisterEvents();

    }
    private void Update() //Opdatering af spillet konstant. Bliver opdateres hvert frame.
    {
        if (!currentCamera) // Hvis vi ikke har et camera danner vi et kamera.
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray,out info, 100, LayerMask.GetMask("Tile","Hover","Highlight")))
        {
            // Fanger indexes at de tiles vi har ramt
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // Hvis vi hover over en tile, efter at vi ikke hoverer en tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // Hvis vi hover over en tile, efter at vi allerede hoverede over en tile
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0)) //Hvis du bruger venstre klik
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null) //Tjekker om du klikker på en brik
                {
                    //Er det vores tur?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam==0) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        // Få en liste over de steder som brikken kan bevæge sig
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        //Få en liste af specielle træk
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();

                        HiglightTiles();

                    }
                }
            }
            if (currentlyDragging!= null && Input.GetMouseButtonUp(0)) //Hvis vi giver slip på venstre klik
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);


                if (ContainsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y)))
                {
                    Moveto(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    //Net implementation
                    NetMakeMove nm = new NetMakeMove();
                    nm.originalX = previousPosition.x;
                    nm.originalY = previousPosition.y;
                    nm.destinationX = hitPosition.x;
                    nm.destinationY = hitPosition.y;
                    nm.teamId = currentTeam;
                    Client.Instance.SendToServer(nm);
                }
                else
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null; // slipper brikken efter et træk
                    RemoveHiglightTiles();
                }

            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") :LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));

                currentlyDragging = null; //Hvis du bevæger cursor ud af boardet slipper den selv.
                RemoveHiglightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset); //Gør det muligt at løfte brikken af brættet
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance)+Vector3.up*dragOffset);
        }
    }



    // Genere board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY) // Funktion som generere vores board tiles
    {
        yOffset += transform.position.y; // Vi sørger for at brikken oprettet over brættet, ikke inden i
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;



        tiles = new GameObject[tileCountX, tileCountY]; // Generere tiles indtil at vi opnår et board med 8x8 eller tilecountX x tilecountY
            for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y) // metode til at generere en single tile
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y)); // generer selve tilet og formaterer hvordan det ses i unity
        tileObject.transform.parent = transform; 

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material=tileMaterial; // Tilføjer udseende materialer

        Vector3[] vertices = new Vector3[4]; // Fire hjørner til vores grid.
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;  // danner første vektor til siden.
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 }; // render vi vektoren i ordren 0,1,2,1,3,2 som skaber 2 trekanter i hvert tile.

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals(); // Fiksere og justere lys

        tileObject.layer = LayerMask.NameToLayer("Tile"); // Tilføjer alle vores tiles til vores lag gruppe "Tile" i Unity

        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //Spawner brikker 
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y]; //instantiere vores brikker

        int whiteTeam = 0, blackTeam = 1;

        //White team

        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);

        for(int i=0; i<TILE_COUNT_X; i++)
        {
            chessPieces[i,1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //Black team

        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type-1],transform).GetComponent<ChessPiece>(); // Instantiere en brik, -1 da vi har en værdi 0, som ikke har en brik type. 

        cp.type = type;
        cp.team = team;

        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    // Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true); // FORCE bestemmer om vi teleportere brikken eller om den bevæger sig langsomt
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x,y),force);



    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Highlight tiles
    private void HiglightTiles()
    {
        for(int i =0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHiglightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

            
        }
        availableMoves.Clear();
    }

    // Skakmat
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true); //Tænder for sejr skærmen
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true); //tænder beskeden om vinderen
    }
    public void OnRematchButton() // Resetter boardet
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);


            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);
        }

    }
    public void GameReset()
    {
        //UI
        rematchButton.interactable = true;

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);


        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Field Reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        //Rengør

        for (int x = 0; x < TILE_COUNT_X; x++) //Fjerner brikker på boardet
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++) //Fjerner de døde hvide brikker
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++) //Fjerner de døde sorte brikker
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();

        Invoke("ShutDownRelay", 1.0f);

        //reset værdier
        playerCount = -1;
        currentTeam = -1;
    }

    //Specielle træk
    public void ProcessSpecialMove()
    {

        //enpassant
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition =moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY-1 | myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);

                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(
                            new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }

            }
        }

        //Forfremmelse
        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if (targetPawn.type == ChessPieceType.Pawn)
            {
                if (targetPawn.team == 0 && lastMove[1].y == 7) //hvid hold
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y,true);
                }
                if (targetPawn.team == 1 && lastMove[1].y == 0) //Sort hold
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
                }
            }
        }

        //Rokering
        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            //venstre tårn
            if (lastMove[1].x==2)
            {
                if (lastMove[1].y == 0) //Hvid hold
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //Sort hold
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            //Højre tårn
            else if(lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) //Hvid hold
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) //Sort hold
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventCheck()
    {
        ChessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        if (chessPieces[x, y].team == currentlyDragging.team)
                            targetKing = chessPieces[x, y];

        //Vi sender tilladte træk, vi sletter derfor træk som vi ikke må lave 
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);

    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //Gem nuværende værdier, til at resette funktions kald
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Gå igennem alle træk og tjæk om vi er i skak
        for(int i = 0; i<moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //Simulerede vi kongens træk
            if(cp.type==ChessPieceType.King)
                kingPositionThisSim=new Vector2Int(simX, simY);

            // Kopier [,] board layoutet ikke en reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>(); // laver en list af angribende brikker
            for(int x = 0; x < TILE_COUNT_X; x++)
            {
                for(int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if(simulation[x, y].team != cp.team)
                            simAttackingPieces.Add(simulation[x, y]); // Tilføjer angribende brik til simulationen af angribende brikker
                    }
                }
            }

            //Simuler træk
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            //Blev en brik taget ved det simulerede træk
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
                simAttackingPieces.Remove(deadPiece); //Fjerner brikken fra vores simulation


            // Få alle de simulerede angribende brikkers mulige træk
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++) //Går igennem alle modstanderens brikker og tilføjer deres mulige træk til simulation
            {
                var piecesMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for(int b = 0; b<piecesMoves.Count; b++)
                {
                    simMoves.Add(piecesMoves[b]);
                }
            }
            //Er kongen under angreb?, hvis ja fjern det som et umuligt træk
            if(ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]); //Tilføj til listen af træk som skal fjernes
            }

            //Restorer det rigtige data til normal
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        //Fjerne dem fra listen.
        for(int i = 0; i < movesToRemove.Count; i++) // Fjern de træk fra listen af mulige træk
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0)? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();

        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                   if (chessPieces[x, y].team == targetTeam)
                   {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x,y].type==ChessPieceType.King)
                            targetKing= chessPieces[x, y];
                   }
                   else
                   {
                        attackingPieces.Add(chessPieces[x, y]);
                   }
                }

        //Er kongen under angreb lige nu?
        List<Vector2Int> currentAvailavleMoves = new List<Vector2Int>();
        for(int i = 0; i < attackingPieces.Count; i++)
        {
            var piecesMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < piecesMoves.Count; b++)
            {
                currentAvailavleMoves.Add(piecesMoves[b]);
            }
        }

        // Er vi i skak lige nu?
        if(ContainsValidMove(ref currentAvailavleMoves,new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            // Kongen er under angreb, kan vi bevæge noget for at hjælpe ham?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                // Da vi sender en reference af træk, kommer den også til at fjerne alle de træk som sætter os i skak
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return false;
            }
            return true; //Skakmat
        }
        return false;
    }
    //Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for(int i =0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    private void Moveto(int originalX, int originalY, int x, int y)
    {
      //  SerialController messagecontroller = new SerialController();



        ChessPiece cp = chessPieces[originalX,originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        //er der en brik på positionen if forvenejen?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team) //tjekker om brikken på positionen er din egen brik.
                return;

            //Hvis det er en fjendtlig brik
            if (ocp.team == 0)
            { //fjerner brikken og sættter den på siden af boardet

                if (ocp.type == ChessPieceType.King)
                    CheckMate(1);
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one*deathSize);
                ocp.SetPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }

        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);
        //messagecontroller.SendSerialMessage("a");

        isWhiteTurn = !isWhiteTurn;
        if (localGame)
            currentTeam = (currentTeam == 0) ? 1 : 0;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        ProcessSpecialMove();
        if(currentlyDragging)
            currentlyDragging = null; // slipper brikken efter et træk
        RemoveHiglightTiles();

        if (CheckForCheckmate())
            CheckMate(cp.team);

        return;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; // Fungere ikke, men denne kode burde aldrig nåes

    }

    #region 
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;


        GameUI.Instance.SetLocalGame += OnSetLocalGame;

    }


    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;


        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
    }
    
    //Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        //Klient er tilsluttet giv ham et hold
        NetWelcome nw = msg as NetWelcome;

        //Giv hold
        nw.AssignedTeam =++playerCount;

        // Giv til kliet

        Server.Instance.SendToClient(cnn, nw);

        if(playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }

    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove nm = msg as NetMakeMove;

        // Modtag besked og send besked tilbage
        Server.Instance.Broadcast(msg);
    }
    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    //Client

    private void OnWelcomeClient(NetMessage msg)
    {
        // Modtager tilslutningsbesked
        NetWelcome nw = msg as NetWelcome;

        // giv hold
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is {nw.AssignedTeam}");

        if(localGame && currentTeam == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void OnStartGameClient(NetMessage obj)
    {
        //ændre kamera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }

    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        Debug.Log($"MM:{mm.teamId}: {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY}");

        if(mm.teamId != currentTeam)
        {
            ChessPiece target = chessPieces[mm.originalX, mm.originalY];

            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

            Moveto(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);

        }
    }
    private void OnRematchClient(NetMessage msg)
    {
        //modtag besked
        NetRematch rm = msg as NetRematch;
        //sæt bool for rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;
        //Aktiver ui
        if (rm.teamId != currentTeam)
        {
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if(rm.wantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }
        //Hvis begge modstandere vil have rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();

    }
    //
    private void ShutDownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }
    private void OnSetLocalGame(bool v)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = v;
    }

    #endregion
}
