using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class MultiplayerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private TMP_InputField ipInput;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName = "PlayerMap";
    [SerializeField] private ushort port = 7777;

    private bool loadingGame = false;

    void OnEnable()
    {
        if (hostButton) hostButton.onClick.AddListener(StartAsHost);
        if (clientButton) clientButton.onClick.AddListener(StartAsClient);
        if (resetButton) resetButton.onClick.AddListener(ResetNetwork);

        HookNetworkEvents();
        SetStatus("Not connected");
    }

    void OnDisable()
    {
        if (hostButton) hostButton.onClick.RemoveListener(StartAsHost);
        if (clientButton) clientButton.onClick.RemoveListener(StartAsClient);
        if (resetButton) resetButton.onClick.RemoveListener(ResetNetwork);

        UnhookNetworkEvents();
    }

    void HookNetworkEvents()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
    }

    void UnhookNetworkEvents()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
    }

    void StartAsHost()
    {
        if (NetworkManager.Singleton == null) { SetStatus("NetworkManager not found"); return; }
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            SetStatus("Already running. Hit Reset first.");
            return;
        }

        ApplyTransportFromUI(isClient: false);

        DisableStartButtons();
        loadingGame = false;

        bool ok = NetworkManager.Singleton.StartHost();
        SetStatus(ok ? "Host started. Waiting for player..." : "StartHost failed (check Console).");
        if (!ok) EnableStartButtons();
    }

    void StartAsClient()
    {
        if (NetworkManager.Singleton == null) { SetStatus("NetworkManager not found"); return; }
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            SetStatus("Already running. Hit Reset first.");
            return;
        }

        ApplyTransportFromUI(isClient: true);

        DisableStartButtons();
        loadingGame = false;

        bool ok = NetworkManager.Singleton.StartClient();
        SetStatus(ok ? "Client starting..." : "StartClient failed (check Console).");
        if (!ok) EnableStartButtons();
    }

    void ApplyTransportFromUI(bool isClient)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            SetStatus("UnityTransport missing on NetworkManager");
            return;
        }

        // For local testing, default to 127.0.0.1 on client
        string ip = (ipInput && !string.IsNullOrWhiteSpace(ipInput.text))
            ? ipInput.text.Trim()
            : (isClient ? "127.0.0.1" : "0.0.0.0");

        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;

        Debug.Log($"[Transport] Address={ip} Port={port}");
    }

    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        // Host/server decides when to load the scene
        if (NetworkManager.Singleton.IsServer)
        {
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            SetStatus($"Player connected ({count}/2)");

            if (!loadingGame && count >= 2)
            {
                loadingGame = true;
                SetStatus("Both players connected. Loading game...");

                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }
        else
        {
            // Client connected to host
            if (clientId == NetworkManager.Singleton.LocalClientId)
                SetStatus("Connected! Waiting for host to start...");
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        // If you disconnect, allow trying again
        loadingGame = false;
        SetStatus("Disconnected. Hit Reset then try again.");
        EnableStartButtons();
    }

    void OnTransportFailure()
    {
        loadingGame = false;
        SetStatus("Transport failure. Hit Reset then try again.");
        EnableStartButtons();
    }

    void ResetNetwork()
    {
        if (NetworkManager.Singleton == null) return;

        loadingGame = false;

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.Shutdown();

        SetStatus("Reset. Not connected.");
        EnableStartButtons();
    }

    void DisableStartButtons()
    {
        if (hostButton) hostButton.interactable = false;
        if (clientButton) clientButton.interactable = false;
    }

    void EnableStartButtons()
    {
        if (hostButton) hostButton.interactable = true;
        if (clientButton) clientButton.interactable = true;
    }

    void SetStatus(string msg)
    {
        if (statusLabel) statusLabel.text = msg;
        Debug.Log("[Menu] " + msg);
    }
}

//using System;
//using Unity.Netcode;
//using Unity.Netcode.Transports.UTP;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;
//using TMPro;
//using System.IO;

//namespace MultiplayerGame
//{
//    public class MultiplayerManager : MonoBehaviour
//    {
//        [Header("UI")]
//        [SerializeField] private Button hostButton;
//        [SerializeField] private Button clientButton;
//        [SerializeField] private TextMeshProUGUI statusLabel;
//        [SerializeField] private string gameSceneName;
//        [SerializeField] private TMP_InputField ipInput;

//        private bool playersConnected = false;

//        void Awake()
//        {
//            Debug.Log("MultiplayerManager running");
//        }

//        void OnEnable()
//        {
//            if (hostButton != null) hostButton.onClick.AddListener(OnHostButtonClicked);
//            if (clientButton != null) clientButton.onClick.AddListener(OnClientButtonClicked);
//        }

//        void OnDisable()
//        {
//            if (hostButton != null) hostButton.onClick.RemoveListener(OnHostButtonClicked);
//            if (clientButton != null) clientButton.onClick.RemoveListener(OnClientButtonClicked);

//            //unsubscribe network callback
//            if (NetworkManager.Singleton != null)
//            {
//                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
//                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
//            }
//        }

//        void Update()
//        {
//            UpdateStatus();
//        }

//        void OnHostButtonClicked()
//        {
//            Debug.Log("Host button clicked");

//            if (NetworkManager.Singleton == null) return;

//            ApplyConnectionData();

//            //subscribe before starting
//            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
//            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

//            bool success = NetworkManager.Singleton.StartHost();

//            if (success)
//            {
//                SetStatusText("Waiting for other player...");
//            }
//            else
//            {
//                SetStatusText("Failed to start Host...");
//            }
//        }

//        // host callback
//        void OnClientConnected(ulong clientId)
//        {
//            if (NetworkManager.Singleton == null) return;
//            if (!NetworkManager.Singleton.IsServer) return;
//            if (playersConnected) return;

//            int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

//            Debug.Log("Client connected. Total players: " + playerCount);

//            if (playerCount >= 2)
//            {
//                playersConnected = true;

//                SetStatusText("Both players connected. Loading game...");

//                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
//            }
//        }

//        void OnClientDisconnected(ulong clientId)
//        {
//            if (NetworkManager.Singleton == null) return;
//            if (!NetworkManager.Singleton.IsServer) return;

//            playersConnected = false;
//            SetStatusText("Player disconnected. Waiting...");
//        }

//        void OnClientButtonClicked()
//        {
//            Debug.Log("Client button clicked");

//            if (NetworkManager.Singleton == null)
//            {
//                SetStatusText("NetworkManager not found");
//                return;
//            }

//            ApplyConnectionData();

//            bool success = NetworkManager.Singleton.StartClient();

//            if (success)
//            {
//                SetStatusText("Starting Client...");
//            }
//            else
//            {
//                SetStatusText("Failed to start Client...");
//            }
//        }

//        void ApplyConnectionData()
//        {
//            if (NetworkManager.Singleton == null) return;

//            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
//            if (transport == null)
//            {
//                SetStatusText("UnityTransport not found on NetworkManager");
//                return;
//            }

//            string ip = (ipInput != null && !string.IsNullOrWhiteSpace(ipInput.text)) ? ipInput.text.Trim() : "127.0.0.1";

//            transport.ConnectionData.Address = ip;

//            Debug.Log($"Transport set to {ip}");
//        }

//        //void OnServerButtonClicked() => NetworkManager.Singleton?.StartServer();

//        void UpdateStatus()
//        {
//            if (NetworkManager.Singleton == null) return;

//            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
//            {
//                SetStatusText("Not connected");
//                return;
//            }

//            //string mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

//            //SetStatusText($"Mode: {mode}");
//        }

//        void SetStatusText(string text)
//        {
//            if (statusLabel != null) statusLabel.text = text;
//        }
//    }
//}
