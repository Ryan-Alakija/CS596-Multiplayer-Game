using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public int winScore = 4;
    
    [SerializeField] private TextMeshProUGUI yourScoreText;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    [SerializeField] private TextMeshProUGUI endgameMessage;

    private PlayerNetwork yourPlayer;
    private PlayerNetwork opponentPlayer;

    private int yourScoreCached;
    private int oppScoreCached;
    private bool gameEnded;

    private void Start()
    {
        StartCoroutine(BindWhenReady());
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    private IEnumerator BindWhenReady()
    {
        //wait until network manager exists and client is connected
        while (NetworkManager.Singleton == null) yield return null;
        while (!NetworkManager.Singleton.IsConnectedClient) yield return null;

        // wait until local player exists
        yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        yourPlayer = localObj.GetComponent<PlayerNetwork>();

        // wait until we can find an opponent player object
        yield return new WaitUntil(() => TryFindOpponent(out opponentPlayer));

        // initial text
        UpdateYour(yourPlayer.Score.Value);
        UpdateOpp(opponentPlayer.Score.Value);

        // subscribe
        yourPlayer.Score.OnValueChanged += OnYourScoreChanged;
        opponentPlayer.Score.OnValueChanged += OnOpponentScoreChanged;
    }

    private bool TryFindOpponent(out PlayerNetwork opp)
    {
        opp = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client == null || client.PlayerObject == null) continue;

            // skip local player
            if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;

            opp = client.PlayerObject.GetComponent<PlayerNetwork>();
            if (opp != null) return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (yourPlayer != null) yourPlayer.Score.OnValueChanged -= OnYourScoreChanged;
        if (opponentPlayer != null) opponentPlayer.Score.OnValueChanged -= OnOpponentScoreChanged;
    }

    private void OnYourScoreChanged(int prev, int cur)
    {
        yourScoreCached = cur;
        UpdateYour(cur);
        //CheckWin();
    }
    private void OnOpponentScoreChanged(int prev, int cur)
    {
        oppScoreCached = cur;
        UpdateOpp(cur);
        //CheckWin();
    }

    private void UpdateYour(int score)
    {
        if (yourScoreText) yourScoreText.text = $"Your score: {score}";
    }

    private void UpdateOpp(int score)
    {
        if (opponentScoreText) opponentScoreText.text = $"Opps score: {score}";
    }

    //private void CheckWin()
    //{
    //    if (gameEnded) return;

    //    if (yourScoreCached >= winScore || oppScoreCached >= winScore)
    //    {
    //        gameEnded = true;

    //        if (endgameMessage != null)
    //        {
    //            endgameMessage.text = (yourScoreCached >= winScore) ? "You won!" : "You lost";
    //        }
    //    }

    //    //optional: stop input/movement here, or trigger a server RPC to end the match

    //}

    public void ShowEndScreen(bool youWon)
    {
        if (endgameMessage == null) return;

        Debug.Log("ShowEndScreen called");

        endgameMessage.text = youWon ? "You won!" : "You lost";

        //hide gameplay HUD parts
        if (yourScoreText) yourScoreText.gameObject.SetActive(false);
        if (opponentScoreText) opponentScoreText.gameObject.SetActive(false);
    }
}
