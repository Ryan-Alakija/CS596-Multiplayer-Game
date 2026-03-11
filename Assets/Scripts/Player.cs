using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15f;

    [Header("Jump Settings")]
    public float jumpForce = 27f;

    [Header("Other classes")]
    public ScoreCounter scoreCounter;

    /*** PRIVATE VARIABLES ***/

    private Rigidbody rb;
    private SpriteRenderer playerSprite;

    private Vector3 movement;
    private float horizontalInput;
    private bool isGrounded;                //check if player is grounded

    private bool wantsToJump;               //jump buffering

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerSprite = GetComponent<SpriteRenderer>();

        wantsToJump = false;

        //find and obtain game objects with the following names, in the scene hierarchy
        GameObject yourScoreGO = GameObject.Find("Player 1 score");
        //GameObject oppsScoreGO = GameObject.Find("Other Score");
        scoreCounter = yourScoreGO.GetComponent<ScoreCounter>();
    }

    //update is called once per frame
    void Update()
    {   
        //check if player "wants to jump"
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            //Debug.Log("Player pressed jump key");
            wantsToJump = true;
        }
    }

    // FixedUpdate - used for more consistent frame rate;
    // optimal for physics related movement
    void FixedUpdate()
    {
        //check if player is grounded before allowing them to jump
        isGrounded = playerIsGrounded();

        //get horizontal input from player and move them accordingly
        horizontalInput = Input.GetAxisRaw("Horizontal");

        //adjust player velocity accordingly
        rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, 0f);

        //jumping
        if (wantsToJump && isGrounded)
        {
            //reset vertical velocity before applying jump force, to ensure consistent jump height
            Vector3 currVelocity = rb.linearVelocity;
            currVelocity.y = 0f;
            rb.linearVelocity = currVelocity;

            //apply upwards force to the player rigidbody;
            //ForceMode.Impulse applies the force instantly
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            isGrounded = false;     //prevent double jumps
        }
        wantsToJump = false;        //reset jump buffer after processing
    }

    //check if player is touching the ground
    public bool playerIsGrounded()
    {
        //cast a ray downwards from player's position, then check if it hits any colliders

        //rayLength - adjust based on player's size;
        //divide by 2 since player's y size is 2 times larger than x size

        RaycastHit hit;
        float rayLength = this.GetComponent<Renderer>().bounds.size.y * 0.5f;
        Debug.Log("Player's size: " + rayLength);

        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            return true;
        }
        return false;
    }

    //check when player touches a coin;
    //destroy the coin and update the score in GameManager
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coin"))
        {
            other.gameObject.SetActive(false);          //deactivate the coin instead of destroying it
            Debug.Log("Player collected a coin!");
            //scoreCounter.score += 1;
        }
    }
}
