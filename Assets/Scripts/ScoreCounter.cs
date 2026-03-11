using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreCounter : MonoBehaviour
{
    [SerializeField] private bool showLocalPlayerScore = true;
    //[SerializeField] private ulong targetClientId;              //only used if showLocalPlayerScore = false
    
    [Header("Dynamic")]

    private TextMeshProUGUI uiText;
    private PlayerNetwork targetPlayer;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        //TryBind();
        StartCoroutine(BindWhenReady());
    }

    IEnumerator BindWhenReady()
    {
        //wait till NetworkManager exists
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        while (!NetworkManager.Singleton.IsConnectedClient)
        {
            yield return null;
        }

        //wait until players are spawned
        yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

        if (showLocalPlayerScore)
        {
            var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            targetPlayer = localPlayerObj.GetComponent<PlayerNetwork>();
        }
        else
        {
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (id != NetworkManager.Singleton.LocalClientId)
                {
                    var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(id);

                    targetPlayer = playerObj.GetComponent<PlayerNetwork>();
                    break;
                }
            }
        }

        if (targetPlayer == null)
        {
            Debug.LogWarning("ScoreCounter: Could not find target player");
            yield break;
        }

        //update immediately + subscribe
        UpdateText(targetPlayer.Score.Value);
        targetPlayer.Score.OnValueChanged += OnScoreChanged;
    }

    //void TryBind()
    //{
    //    if (NetworkManager.Singleton == null) return;

    //    NetworkObject playerObj = null;

    //    if (showLocalPlayerScore)
    //    {
    //        playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
    //    }
    //    else
    //    {
    //        playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);
    //    }

    //    if (playerObj == null) return;

    //    player = playerObj.GetComponent<PlayerNetwork>();
    //    if (player == null) return;

    //    //update immediately + subscribe
    //    UpdateText(player.Score.Value);
    //    player.Score.OnValueChanged += OnScoreChanged;
    //}

    void OnDestroy()
    {
        if (targetPlayer != null)
        {
            targetPlayer.Score.OnValueChanged -= OnScoreChanged;
        }
    }

    void OnScoreChanged(int previous, int current) => UpdateText(current);

    void UpdateText(int score)
    {
        //uiText.text = (showLocalPlayerScore ? "Your Score: " : "Other score: ") + score.ToString("#,0");
        uiText.text = "Score: " + score.ToString("#,0");
    }
}
