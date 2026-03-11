using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    /* SOME NOTES:
     * Who reads input?
     *      only the owning client should read input for their player,
     *      then send input to the server (by ServiceRPC) to be validated and applied to the player's position
     * Who applies movement and physics?
     *      the server should apply movement and physics to all players
     * Who owns the score + coins?
     *      coins: server decides coin spawns and coin pickups
     *      score: stored on server (NetworkVariable) and displayed on clients (ClientRPC)
     */

    [Header("Movement Settings")]
    public float moveSpeed = 15f;

    [Header("Jump Settings")]
    public float jumpForce = 27f;

    [Header("Other classes")]
    public ScoreCounter scoreCounter;

    [Header("Ground check")]
    [SerializeField] private LayerMask platformMask;    //layer mask for the platforms

    public static bool GameOver;
    
    /*** PRIVATE VARIABLES ***/

    private Rigidbody rb;

    private float horizontalInput;          //store horizontal input from player
    private bool jumpPressed;               //jump buffering - activated when player presses jump key

    //Network variables - sync with everyone;
    //can be read by everyone, but only updated by the server
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GameOver = false;
    }

    public override void OnNetworkSpawn()
    {
        //make sure 2D-in-3D constraints are applied
        rb.constraints = 
            RigidbodyConstraints.FreezePositionZ | 
            RigidbodyConstraints.FreezeRotationX | 
            RigidbodyConstraints.FreezeRotationY | 
            RigidbodyConstraints.FreezeRotationZ;

        //only the server simulates physics; clients follow replicated transform
        if (!IsServer)
        {
            //rb.isKinematic = true;      //set each client rb as kinematic
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;     //only the owning client should read input for their player

        //collect input on owner only
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            jumpPressed = true;
        }
    }

    // FixedUpdate - used for more consistent frame rate;
    // optimal for physics related movement
    void FixedUpdate()
    {
        if (!IsOwner) return;

        //check if player is grounded before allowing them to jump
        //isGrounded = playerIsGrounded();

        horizontalInput = Input.GetAxisRaw("Horizontal");       //get horizontal input from player and move them accordingly
        bool wantsToJump = jumpPressed;                         //store jump input in a separate variable
        jumpPressed = false;                                    //reset jump buffer

        //send input to server
        SubmitInputServerRpc(horizontalInput, wantsToJump);
    }

    [Rpc(SendTo.Server)]
    void SubmitInputServerRpc(float horizontalInput, bool wantsToJump)
    {
        //server applies movement
        rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, 0f);

        //Debug.Log(IsGroundedServer() ? "Player is grounded" : "Player is not grounded");
        //Debug.Log(wantsToJump ? "Player pressed jump button" : "Player did not press jump button");

        if (wantsToJump && IsGroundedServer())
        {
            //reset vertical velocity before applying jump force, to ensure consistent jump height
            Vector3 currVelocity = rb.linearVelocity;
            currVelocity.y = 0f;
            rb.linearVelocity = currVelocity;

            //apply upwards force to the player rigidbody;
            //ForceMode.Impulse applies the force instantly
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    bool IsGroundedServer()
    {
        //cast a ray downwards from player's position, then check if it hits any colliders

        //rayLength - adjust based on player's size;
        //divide by 2 since player's y size is 2 times larger than x size

        RaycastHit hit;
        float rayLength = this.GetComponent<Renderer>().bounds.size.y * 0.5f;
        //Debug.Log("Player's size: " + rayLength);

        return Physics.Raycast(transform.position, Vector3.down, out hit, rayLength);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;     //only the server should handle coin pickups

        if (other.CompareTag("Coin"))
        {
            var networkObj = other.GetComponent<NetworkObject>();
            if (networkObj != null) networkObj.Despawn(true);
            
            Score.Value += 1;

            Debug.Log($"Score: {Score.Value}");

            if (Score.Value >= 10 && !GameOver)
            {
                Debug.Log("Game Over");
                GameOver = true;
                EndGameServer();
            }
        }
    }

    void EndGameServer()
    {
        Debug.Log(IsServer ? "EndGameServer called" : "Not a server");
        
        if (!IsServer) return;

        Debug.Log($"Telling everyone to show the end screen (OwnerClientId = {OwnerClientId}");

        //tell all clients to show the end screen
        ShowEndScreenClientRpc(OwnerClientId);

        //start despawning, but make it a coroutine so it doesn't overlap with other endgame functions
        StartCoroutine(DespawnGameplayAfterDelay());
    }

    IEnumerator DespawnGameplayAfterDelay()
    {
        yield return null;

        var spawned = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjectsList);

        //despawn all coins + players(server authoritative)
        foreach (var netObj in spawned)
        {
            if (netObj == null) continue;

            //keep NetworkManager / scene objects that aren't NetworkObjects out of this list
            if (netObj.CompareTag("Coin") || netObj.CompareTag("Player"))
            {
                netObj.Despawn(true);
            }
        }

        //Despawn coin spawner (check if it exists AND is a network object)
        var coinSpawner = FindFirstObjectByType<CoinSpawner>();
        if (coinSpawner != null)
        {
            var spawnerNetObj = coinSpawner.GetComponent<NetworkObject>();
            if (spawnerNetObj != null && spawnerNetObj.IsSpawned)
            {
                spawnerNetObj.Despawn(true);
            }
            else
            {
                coinSpawner.gameObject.SetActive(false);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ShowEndScreenClientRpc(ulong winnerClientId)
    {
        // winnerClientId is the player who triggered game over (coin pickup)
        bool youWon = NetworkManager.Singleton.LocalClientId == winnerClientId;

        Debug.Log($"ShowsEndScreenClientRpc called (winnerClientId = {winnerClientId})");

        // Find your ScoreUI and show it
        var ui = FindFirstObjectByType<ScoreUI>();
        if (ui != null) ui.ShowEndScreen(youWon);
    }
}
