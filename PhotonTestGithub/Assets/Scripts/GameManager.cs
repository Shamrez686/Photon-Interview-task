using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/**
 * Holds the current state of the game, and updates the display.
 * 
 * Detects the winning condition.
 */
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable, IOnEventCallback
{
    #region Constants

    // Event: the remote player has clicked on a cell at (row, column)
    public const int EVENT_MOVE = 1;

    // Possible turns and states of a cell
    public enum MarkType { EMPTY, X, O, TIE }

    // Number of columns and rows of the grid
    public const int Size = 3;

    #endregion

    #region Inspector-based configuration

    // Text label we will use to display state
    public TextMeshProUGUI turnText;

    #endregion

    #region Shared game state

    // Current turn in the game
    private MarkType _turn;
    public MarkType Turn {
        get
        {
            return _turn;
        }
        private set {
            _turn = value;
            if (Winner == MarkType.EMPTY)
            {
                if (PhotonNetwork.IsConnected)
                {
                    turnText.text = MyTurn == Turn
                        ? $"Your turn, {PhotonNetwork.NickName}"
                        : $"Waiting for {GetOpponent().NickName}";
                }
                else
                {
                    turnText.text = $"Turn: {value}";
                }
            }
        }
    }

    // Winner of the game
    private MarkType _winner;
    public GameObject HomeButton;
    public MarkType Winner
    {
        get
        {
            return _winner;
        }
        private set
        {
            _winner = value;

            switch (value)
            {
                case MarkType.O:
                case MarkType.X:
                    string winnerName;
                    if (PhotonNetwork.IsConnected)
                    {
                        winnerName = MyTurn == value
                            ? PhotonNetwork.NickName
                            : GetOpponent().NickName;
                    }
                    else
                    {
                        winnerName = value.ToString();
                    }

                    turnText.text = photonView.IsMine
                        ? $"Winner: {winnerName}!"
                        : $"Winner: {winnerName}!";

                    HomeButton.SetActive(true);
                    break;

                case MarkType.TIE:
                    turnText.text = photonView.IsMine
                        ? "Tied! - "
                        : "Tied! - ";
                    HomeButton.SetActive(true);

                    break;
            }
        }
    }

    // Access to cells
    private List<GridCell> cells = new List<GridCell>();

    #endregion

    #region Private game state

    // Turn of the connected player (if playing online)
    private MarkType MyTurn;

    #endregion

    #region Initialisation and input handling

    void Start()
    {
        if (photonView.IsMine)
        {
            Winner = MarkType.EMPTY;
            MyTurn = MarkType.O;
            Turn = MarkType.O;
        }
        else
        {
            MyTurn = MarkType.X;
        }
    }

    void Update()
    {
        if (photonView.IsMine && Winner != MarkType.EMPTY && Input.GetKeyDown(KeyCode.Space))
        {
            // The master player can reset the scene
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    #endregion

    #region Game event handling

    public void OnCellCreated(GridCell cell)
    {
        cells.Add(cell);
        cell.Clicked.AddListener(OnCellClicked);
    }

    public void OnCellClicked(GridCell cell)
    {
        if (Winner != MarkType.EMPTY)
        {
            // Game has finished, do nothing
            return;
        }
        else if (PhotonNetwork.IsConnected && Turn != MyTurn)
        {
            // We are in an online game, and it's not your turn!
            return;
        }

        if (photonView.IsMine)
        {
            // Really change the cell
            CellPlayed(cell);
        }
        else
        {
            // Send the move, but don't change the cell:
            // we do not own the game state.
            PhotonNetwork.RaiseEvent(EVENT_MOVE,
                new int[] { cell.Row, cell.Column },
                RaiseEventOptions.Default,
                SendOptions.SendReliable);
        }
    }

    private void CellPlayed(GridCell cell)
    {
        if (cell.Mark == MarkType.EMPTY)
        {
            cell.Mark = Turn;
            DetectVictoryConditionAround(cell.Row, cell.Column);
            Turn = Turn == MarkType.O ? MarkType.X : MarkType.O;
        }
    }

    #endregion

    #region Photon event handling and synchronisation

    public Player GetOpponent()
    {
        return PhotonNetwork.CurrentRoom.Players.Values.First(e => !e.IsLocal);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonView.IsMine)
        {
            switch (photonEvent.Code)
            {
                case EVENT_MOVE:
                    int[] data = (int[])photonEvent.CustomData;
                    int row = data[0];
                    int col = data[1];
                    CellPlayed(cells[row * Size + col]);
                    break;
            }
        }
    }

    public override void OnLeftRoom()
    {
       /// on left
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        // The other player left - might as well leave, too!
        //PhotonNetwork.LeaveRoom();
    }

    public void backtolobby()
    { 
      if (PhotonNetwork.InRoom)
        PhotonNetwork.LeaveRoom();

        PhotonNetwork.LoadLevel("Lobby");
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.Winner);
            stream.SendNext(this.Turn);

            foreach (GridCell cell in cells)
            {
                stream.SendNext(cell.Mark);
            }
        }
        else
        {
            this.Winner = (MarkType) stream.ReceiveNext();
            this.Turn = (MarkType)stream.ReceiveNext();

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].Mark = (MarkType) stream.ReceiveNext();
            }
        }
    }

    #endregion

    #region Cell accessors

    private int GetCellIndex(GridCell cell)
    {
        return GetCellIndex(cell.Row, cell.Column);
    }

    private int GetCellIndex(int row, int column)
    {
        return row * Size + column;
    }

    #endregion

    #region Victory detection

    private void DetectVictoryConditionAround(int row, int column)
    {
        if (DetectVictoryConditionByRow(row, column)
            || DetectVictoryConditionByColumn(column, column)
            || DetectVictoryConditionByMajorDiagonal(row, column)
            || DetectVictoryConditionByMinorDiagonal(row, column))
        {
            int i = GetCellIndex(row, column);
            Winner = cells[i].Mark;
        }
        else if (DetectTie())
        {
            Winner = MarkType.TIE;
        }
    }

    private bool DetectVictoryConditionByMinorDiagonal(int row, int column)
    {
        if (row != Size - 1 - column)
        {
            // Cell is not part of minor diagonal
            return false;
        }

        int i = GetCellIndex(row, column);
        for (int j = 0; j < Size; ++j)
        {
            if (cells[GetCellIndex(j, Size - 1 - j)].Mark != cells[i].Mark)
            {
                return false;
            }
        }
        return true;
    }

    private bool DetectVictoryConditionByMajorDiagonal(int row, int column)
    {
        if (row != column)
        {
            // Cell is not part of major diagonal
            return false;
        }

        int i = GetCellIndex(row, column);
        for (int j = 0; j < Size; ++j)
        {
            if (cells[GetCellIndex(j, j)].Mark != cells[i].Mark)
            {
                return false;
            }
        }
        return true;
    }

    private bool DetectVictoryConditionByColumn(int row, int column)
    {
        int i = GetCellIndex(row, column);
        for (int iRow = 0; iRow < Size; ++iRow)
        {
            if (cells[GetCellIndex(iRow, column)].Mark != cells[i].Mark)
            {
                return false;
            }
        }
        return true;
    }

    private bool DetectVictoryConditionByRow(int row, int column)
    {
        int i = GetCellIndex(row, column);
        for (int iCol = 0; iCol < Size; ++iCol)
        {
            if (cells[GetCellIndex(row, iCol)].Mark != cells[i].Mark)
            {
                return false;
            }
        }
        return true;
    }

    private bool DetectTie()
    {
        for (int j = 0; j < cells.Count; ++j)
        {
            if (cells[j].Mark == MarkType.EMPTY)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}
