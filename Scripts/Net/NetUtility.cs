using System;  // henviser til .NET Core system biblioteket for at anvende dets klasser og funktioner.
using Unity.Networking.Transport;  //henviser til Unity's transport-system bibliotek til brug i netværkskommunikation
using UnityEngine;  //henviser til Unity's primære spil bibliotek til brug af en række spil relaterede klasser og funktioner.

public enum OpCode  //definition af en offentlig enum kaldet "OpCode", hvor navngivne konstanter defineres til at identificere specifikke typer af netværksbeskeder.
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5
}
public static class NetUtility  //En definition af en offentlig statisk klasse kaldet "NetUtility", hvor metoder og handlinger er defineret til netværkskommunikation.
{
    public static void OnData(DataStreamReader stream, NetworkConnection cnn, Server server = null)  //En offentlig statisk metode kaldet "OnData" der tager tre argumenter: "stream", "cnn", og "server". Metoden læser en opkodetype fra streamen, og afhængig af opkoden skabes en bestemt type af netværksbesked og afsendes.
    {
        NetMessage msg = null;
        Debug.Log("stream = " + stream);  //bruger Unity's Debug.Log funktion til at skrive en besked til output-konsollen med værdien af streamen.


        var opCode = (OpCode)stream.ReadByte();   //læser det første byte fra streamen og typecaster det til en opkode fra enum "OpCode".
        switch (opCode)     //En "switch"-konstruktion, der afhængigt af værdien af "opCode", instansierer en bestemt type af netværksbesked.
        {
            case OpCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); break;
            case OpCode.WELCOME: msg = new NetWelcome(stream); break;
            case OpCode.START_GAME: msg = new NetStartGame(stream); break;
            case OpCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
            case OpCode.REMATCH: msg = new NetRematch(stream); break;
            default:
                Debug.LogError("Message received had no OpCode");       //skriver en fejlbesked til output-konsollen, hvis en modtaget besked ikke har en gyldig opkode.
                break;
        }

        if (server != null)     //En "if"-konstruktion der bestemmer, om en besked skal håndteres på serveren eller på klienten, baseret på om "server" argumentet er sat til null eller ej.
            msg.RecievedOnServer(cnn);
        else
            msg.ReceivedOnClient();
    }

    //Net Messages
    public static Action<NetMessage> C_KEEP_ALIVE;      //Definitioner af offentlige statiske handlinger, der er tilknyttet hver type af netværksbesked. Handlingerne bliver udført, når en besked af denne type modtages.
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;
    public static Action<NetMessage, NetworkConnection> S_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> S_WELCOME;
    public static Action<NetMessage, NetworkConnection> S_START_GAME;
    public static Action<NetMessage, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> S_REMATCH;

}
