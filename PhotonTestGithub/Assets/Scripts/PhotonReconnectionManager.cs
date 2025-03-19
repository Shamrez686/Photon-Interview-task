using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.SceneManagement;

public class PhotonReconnectionManager : MonoBehaviourPunCallbacks
{
    private bool isReconnecting = false;
    private int reconnectAttempts = 0;
    private const int maxReconnectAttempts = 10;
    private const float reconnectDelay = 3f;

    [SerializeField] private GameObject reconnectingUI;
    [SerializeField] private GameObject reconnectingOtherUI;

    void Start()
    {
        if (reconnectingUI) reconnectingUI.SetActive(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!isReconnecting)
        {
            Debug.LogWarning("Disconnected from Photon: " + cause);
            StartCoroutine(TryReconnect());
        }
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        reconnectingOtherUI.SetActive(true);
        StartCoroutine(WaitForOtherPlayer());
    }

    private IEnumerator WaitForOtherPlayer()
    {
        yield return new WaitForSeconds(30);
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("Lobby");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            reconnectingOtherUI.SetActive(false);
        }
    }

    private IEnumerator TryReconnect()
    {
        isReconnecting = true;
        reconnectAttempts = 0;
        if (reconnectingUI) reconnectingUI.SetActive(true);

        while (reconnectAttempts < maxReconnectAttempts)
        {
            Debug.Log($"Reconnect Attempt {reconnectAttempts + 1}...");

            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("Trying to reconnect to Photon...");
                PhotonNetwork.Reconnect(); // First, reconnect to Photon Master Server
                yield return new WaitForSeconds(reconnectDelay);
            }

            if (PhotonNetwork.IsConnectedAndReady)
            {
                if (PhotonNetwork.InRoom)
                {
                    Debug.Log("Rejoined the room successfully.");
                    StopReconnection();
                    yield break;
                }
                else
                {
                    Debug.Log("Reconnected to Photon, now trying to rejoin room...");
                    PhotonNetwork.RejoinRoom(ConnectManager.lastRoomName);
                    yield return new WaitForSeconds(reconnectDelay);
                }
            }

            reconnectAttempts++;
        }

        if (!isReconnecting)
        {
            Debug.LogWarning("Reconnection failed! Returning to lobby...");
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("Lobby");
        }
    }
    private string lastRoomName; // Store room name upon joining
    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully rejoined the room.");
       
        Debug.Log("lastRoomName. " + lastRoomName);

        //StopReconnection();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Failed to rejoin room: {message}");
        //PhotonNetwork.LoadLevel("Lobby"); // Redirect to lobby if room no longer exists
    }

    public override void OnConnectedToMaster()
    {
        if (isReconnecting && !PhotonNetwork.InRoom)
        {
            Debug.Log("Connected to Master Server, but not in room. Attempting to rejoin...");
            PhotonNetwork.JoinLobby();
        }
    }

    private void StopReconnection()
    {
        isReconnecting = false;
       // reconnectAttempts = maxReconnectAttempts;
        if (reconnectingUI) reconnectingUI.SetActive(false);
    }
}
