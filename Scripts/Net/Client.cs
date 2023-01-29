using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;

public class Client : MonoBehaviour
{
    #region Singleton implementation
    public static Client Instance { get; set; }

    private void Awake()
    {
        Instance = this;
    }
    #endregion


    public NetworkDriver driver;
    private NetworkConnection connection;

    private bool isActive = false;

    public Action connectionDropped;

    //Methods
    public void Init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndPoint endpoint = NetworkEndPoint.Parse(ip, port); //Alle ipadresser kan connecte til serveren
        endpoint.Port = port;

        connection = driver.Connect(endpoint);

        Debug.Log("Attempting to connect to server on" + endpoint.Address);

        isActive = true;

        RegisterToEvent();
    }
    public void Shutdown()
    {
        if (isActive)
        {
            UnregisterToEvent();
            driver.Dispose();
            isActive = false;
            connection = default(NetworkConnection);
        }
    }
    public void OnDestroy()
    {
        Shutdown();
    }

    public void Update()
    {
        if (!isActive)
            return;

        driver.ScheduleUpdate().Complete();
        CheckAlive();

        UpdateMessagePump(); // er der nogen der sender os en besked
    }
    private void CheckAlive()
    {
        if(!connection.IsCreated && isActive)
        {
            Debug.Log("Something went wrong, lost connection to server");
            connectionDropped?.Invoke();
            Shutdown();
        }
    }


    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        
         NetworkEvent.Type cmd;
         while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
              if (cmd == NetworkEvent.Type.Connect)
              {
                SendToServer(new NetWelcome());
                Debug.Log("Vi er tilsluttet JJAAAA");
              }
              else if (cmd == NetworkEvent.Type.Data)
              {
                Debug.Log("stream = " + stream);

               NetUtility.OnData(stream, default(NetworkConnection));
              }
              else if (cmd == NetworkEvent.Type.Disconnect)
              {
                Debug.Log("Client got disconnected from server");
                connection = default(NetworkConnection);
                connectionDropped?.Invoke();
                Shutdown();
              }
            }
        
    }

    public void SendToServer(NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    //EventParsing

    private void RegisterToEvent()
    {
       NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnregisterToEvent()
    {
       NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }

    private void OnKeepAlive(NetMessage nm)
    {
        // Send tilbage
        SendToServer(nm);
    }
}
