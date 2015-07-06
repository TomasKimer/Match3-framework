using UnityEngine;
using System.Collections;

/// <summary>
/// Adding animations and other functionality to the GameObject on the board.
/// </summary>
public class BoardItem : MonoBehaviour {

    /// <summary>
    /// Possible item states.
    /// </summary>
    public enum ItemState {
        Idle,
        AnimatingMove,
        AnimatingDestroy,
        Destroyed
    }

    /// <summary>
    /// Gets item state.
    /// </summary>
    public ItemState State {
        get { return currentState; }
    }

    /// <summary>
    /// Gets/sets x-item position on the board.
    /// </summary>
    public int X {
        get { return x; }
        set { x = value; }
    }

    /// <summary>
    /// Gets/sets y-item position on the board.
    /// </summary>
    public int Y {
        get { return y; }
        set { y = value; }
    }

    /// <summary>
    /// Current item state.
    /// </summary>
    private ItemState currentState = ItemState.Idle;

    // Item position on the board for picking.
    private int x;
    private int y;

    // Move animation variables.
    private Vector3 moveNewPos;
    private Vector3 moveStartPos;
    private float moveLen;
    private float moveSpeed;
    private float moveStartTime;
    private bool moveAnimPrepared = false; // for deferred start of an animation (not used)
    private bool moveBack; // reverses the animation after move was finished (not used)

    // Destroy animation variables (item disappears).
    private float destroySpeed;
    private float destroyStartTime;
    private Color destroyFinalColor;

    /// <summary>
    /// Original color of a GameObject material.
    /// </summary>
    private Color originalColor;

    /// <summary>
    /// Reference to the GameController (for check purpose only).
    /// </summary>
    private GameController gameController;


    /// <summary>
    /// Use this for initialization.
    /// </summary>
    void Start() {
        originalColor = renderer.material.color;

        // Check if GameController is present.
        gameController = FindObjectOfType(typeof(GameController)) as GameController;
        if (gameController == null) {
            Debug.LogError("Match3 GameController is not present!");
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update() {

        switch (currentState) {

            // Move animation.
            case ItemState.AnimatingMove:

                float distCovered = (Time.time - moveStartTime) * moveSpeed;
                float fracJourney = distCovered / moveLen;

                transform.position = Vector3.Lerp(moveStartPos, moveNewPos, fracJourney);

                // Animation finished.
                if (fracJourney >= 1.0f) {
                    // Item has to move back (not used).
                    if (moveBack) {
                        moveNewPos = moveStartPos;
                        moveStartPos = transform.position;
                        moveStartTime = Time.time;

                        moveBack = false;
                    }
                    // End of animation.
                    else {
                        currentState = ItemState.Idle;
                    }
                }
                break;

            // Destroy animation (item disappears).
            case ItemState.AnimatingDestroy:

                float amount = (Time.time - destroyStartTime) * destroySpeed;
                renderer.material.color = Color.Lerp(originalColor, destroyFinalColor, amount);

                // Animation finished.
                if (amount >= 1.0f) {
                    currentState = ItemState.Destroyed;
                }
                break;
        }
    }

    /// <summary>
    /// Resets the color of a GameObject material.
    /// </summary>
    public void ResetColor() {
        renderer.material.color = originalColor;
    }

    /// <summary>
    /// Begins move animation of an item.
    /// </summary>
    /// <param name="newPos">New position.</param>
    /// <param name="speed">Speed.</param>
    /// <param name="moveBack">If set to <c>true</c>, reverses the animation after move was finished.</param>
    public void AnimateMove(Vector3 newPos, float speed, bool moveBack = false) {
        PrepareMoveAnimation(newPos, speed, moveBack);
        StartMoveAnimation();
    }

    /// <summary>
    /// Prepares the move animation (animation can be started later).
    /// </summary>
    /// <param name="newPos">New position.</param>
    /// <param name="speed">Speed.</param>
    /// <param name="moveBack">If set to <c>true</c>, reverses the animation after move was finished.</param>
    public void PrepareMoveAnimation(Vector3 newPos, float speed, bool moveBack = false) {
        moveNewPos = newPos;
        moveStartPos = transform.position;
        moveLen = Vector3.Distance(moveStartPos, moveNewPos);
        moveSpeed = speed;
        this.moveBack = moveBack;

        moveAnimPrepared = true;
    }

    /// <summary>
    /// Starts the move animation.
    /// </summary>
    public void StartMoveAnimation() {
        if (!moveAnimPrepared) {
            return;
        }
        moveStartTime = Time.time;
        moveAnimPrepared = false;
        currentState = ItemState.AnimatingMove;
    }

    /// <summary>
    /// Starts the destroy animation (item disappears).
    /// </summary>
    /// <param name="speed">Speed.</param>
    public void AnimateDestroy(float speed) {
        destroySpeed = speed;
        destroyStartTime = Time.time;
        destroyFinalColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.0f);

        currentState = ItemState.AnimatingDestroy;
    }
}
