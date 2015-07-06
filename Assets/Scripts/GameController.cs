using UnityEngine;
using System.Collections;

/// <summary>
/// Simple Match 3 game controller/view.
/// </summary>
public class GameController : MonoBehaviour {

    /// <summary>
    /// GameObjects representing the various board items (must have BoardItem script attached).
    /// Dimensions of all board items are taken from the first one in array (for spacing etc.).
    /// </summary>
    public BoardItem[] boardItems;

    /// <summary>
    /// GameObject representing a highlight of possible move (hint).
    /// </summary>
    public GameObject possibleMoveHighlight;

    /// <summary>
    /// Board width.
    /// </summary>
    public int boardSizeX = 8;

    /// <summary>
    /// Board height.
    /// </summary>
    public int boardSizeY = 8;

    /// <summary>
    /// Board items spacing.
    /// </summary>
    public Vector2 itemSpacing = new Vector2(0.1f, 0.1f);

    /// <summary>
    /// Distance of drag required to trigger item swap.
    /// </summary>
    public float dragLength = 25.0f;

    // Animation speeds.
    public float swapAnimSpeed = 5.0f;
    public float dropAnimSpeed = 10.0f;
    public float destroyAnimSpeed = 5.0f;


    /// <summary>
    /// Drag direction for swaping board items by touch/mouse.
    /// </summary>
    private enum DragDirection {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Possible states of the game.
    /// </summary>
    private enum GameState {
        Idle,
        MoveCheck,
        MatchOrPossibleGet,
        ItemGenerateAndDrop,
        GameOver
    }

    /// <summary>
    /// Current state of the game.
    /// </summary>
    private GameState gameState = GameState.Idle;

    /// <summary>
    /// References to all items on the board (for fast lookup).
    /// </summary>
    private BoardItem[,] boardAll;

    /// <summary>
    /// Center of the board.
    /// </summary>
    private Vector3 boardCenter;

    /// <summary>
    /// Size of the items on the board (taken from the first GameObject).
    /// </summary>
    private Vector3 itemSize;

    /// <summary>
    /// First selected item.
    /// </summary>
    private BoardItem selObj1;

    /// <summary>
    /// Second selected item.
    /// </summary>
    private BoardItem selObj2;

    /// <summary>
    /// The drag origin (from the first selected item).
    /// </summary>
    private Vector2 dragOrigin;

    /// <summary>
    /// Instance of a possibleMoveHighlight GameObject (prefab).
    /// </summary>
    private GameObject possibleHighlightObj = null;


    /// <summary>
    /// Match 3 game logic.
    /// </summary>
    private Match3 match3;


    /// <summary>
    /// Initialization.
    /// </summary>
    void Start() {
        // GUI scaling.
        GUIScaler.Initialize();

        // Game logic.
        match3 = new Match3();

        // Start new game.
        NewGame();
    }

    /// <summary>
    /// On GUI.
    /// </summary>
    void OnGUI() {
        GUIScaler.Begin();

        // New game button.
        if (GUI.Button(new Rect(10, 10, 150, 100), "New Game")) {
            NewGame();
        }

        // Score label.
        string score = "Score: " + match3.Score.ToString();
        if (gameState == GameState.GameOver) {
            score = "Game Over!, " + score;
        }
        GUI.Label(new Rect(10, 120, 150, 50), score);

        GUIScaler.End();
    }

    /// <summary>
    /// Performs game update and touch and mouse input.
    /// </summary>
    void Update() {
        // Back button.
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();


        // Wait for all animations to finish - TODO optimize.
        if (!ItemsIdle()) {
            return;
        }

        // ------ Game Update -------
        switch (gameState) {

            // Check if move has been valid.
            case GameState.MoveCheck:
                if (match3.CheckMove(selObj1.X, selObj1.Y, selObj2.X, selObj2.Y)) {
                    // Move has been valid, swap objects (animation has only changed real positions).
                    SwapObjects(selObj1, selObj2, false);
                    possibleHighlightObj.renderer.enabled = false;
                    gameState = GameState.MatchOrPossibleGet;
                }
                else {
                    // Move hasn't been valid, animate items back.
                    selObj1.AnimateMove(selObj2.transform.position, swapAnimSpeed);
                    selObj2.AnimateMove(selObj1.transform.position, swapAnimSpeed);
                    gameState = GameState.Idle;
                }

                // Clear selection.
                selObj1 = null;
                selObj2 = null;

                break;


            // Get matches or possible moves (and check for game over).
            case GameState.MatchOrPossibleGet:
                // Get matches.
                ArrayList matches = match3.GetMatches();
                if (matches.Count == 0) {
                    // No matches found, check for possible moves.
                    ArrayList possibleMoves = match3.GetPossibleMoves();
                    if (possibleMoves.Count > 0) {
                        // There are some possible moves, highlight one of them (by random).
                        Match3.Point p = possibleMoves[Random.Range(0, possibleMoves.Count)] as Match3.Point;
                        ShowPossibleMove(p.x, p.y);
                        gameState = GameState.Idle;
                    }
                    else {
                        // No possible moves found, game is over.
                        gameState = GameState.GameOver;
                    }
                }
                else {
                    // Matches found.
                    foreach (ArrayList match in matches) {
                        foreach (Match3.Point p in match) {
                            // Animate destroy of matched items. 
                            boardAll[p.x, p.y].AnimateDestroy(destroyAnimSpeed);
                        }
                    }
                    gameState = GameState.ItemGenerateAndDrop;
                }
                break;


            // Generate new items and perform drop.
            case GameState.ItemGenerateAndDrop:
                // Get drop swaps of existing items.
                ArrayList dropSwaps = match3.GetDropSwaps();

                // Begin drop animations and swap items data.
                foreach (Match3.Point[] p in dropSwaps) {
                    BoardItem bi1 = boardAll[p[0].x, p[0].y];
                    BoardItem bi2 = boardAll[p[1].x, p[1].y];

                    bi1.AnimateMove(GetPosOnBoard(p[1].x, p[1].y), dropAnimSpeed);
                    SwapObjects(bi1, bi2, false); // Swap without positions.
                }

                // Get positions of destroyed items (already dropped) and generate new ones.
                ArrayList destroyed = match3.GetDestroyedItemsAndGenerateNew();
                int lastX = -1, firstDestroyedY = -1;

                // Destroy old items, create new ones (above the board) and begin drop animations of them.
                foreach (Match3.Point p in destroyed) {
                    Destroy(boardAll[p.x, p.y].gameObject);

                    // Get y-coord where to put new items above the board (array is sorted,
                    // going through every column from left to right, bottom to top.
                    if (p.x != lastX) {
                        // New column.
                        firstDestroyedY = p.y;
                        lastX = p.x;
                    }

                    // Create item with adjusted y-position.
                    BoardItem clone = CreateBoardItem(p.x, p.y, 0, boardSizeY - firstDestroyedY);
                    // Begin move animation to the right position.
                    clone.AnimateMove(GetPosOnBoard(p.x, p.y), dropAnimSpeed);
                }

                // Check for the new matches.
                gameState = GameState.MatchOrPossibleGet;

                break;
        }


        // ----------- Input ----------
        // Touch input.
        if (Input.touchCount == 1) {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase) {

                case TouchPhase.Began:
                    MyMouseDown(touch.position);
                    break;

                case TouchPhase.Moved:
                    MyMouseMove(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    MyMouseUp(touch.position);
                    break;
            }
        }
        // Mouse input.
        else {
            if (Input.GetMouseButtonDown(0)) {
                MyMouseDown(Input.mousePosition);
            }
            else if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) {
                MyMouseMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0)) {
                MyMouseUp(Input.mousePosition);
            }
        }
    }

    /// <summary>
    /// Checks if all BoardItems are idle (finished their animations).
    /// </summary>
    /// <returns><c>true</c> if all items are idle (not animating).</returns>
    bool ItemsIdle() {
        foreach (BoardItem bi in boardAll) {
            if (bi.State != BoardItem.ItemState.Idle &&
                bi.State != BoardItem.ItemState.Destroyed) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Starts a new game with current settings (public fields).
    /// Note: Dimensions of all board items are taken from the first one (boardItems[0]).
    /// </summary>
    void NewGame() {
        // Make new board (logic).
        match3.MakeNewBoard(boardSizeX, boardSizeY, boardItems.Length);

        // Init game view.        
        boardAll = new BoardItem[boardSizeX, boardSizeY];
        itemSize = boardItems[0].renderer.bounds.size;
        Vector3 boardSize = new Vector3(boardSizeX * itemSize.x + ((boardSizeX - 1) * itemSpacing.x),
                                        boardSizeY * itemSize.y + ((boardSizeY - 1) * itemSpacing.y), itemSize.z);
        boardCenter = boardSize / 2.0f;
        RefreshBoard();
    }

    /// <summary>
    /// Recreates the view of game board according the current state of game logic.
    /// Called only by NewGame().
    /// </summary>
    void RefreshBoard() {
        // Destroy old items.
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
        // GameObject is not set to null immediately.
        possibleHighlightObj = null;

        // Create new items.
        for (int y = 0; y < boardSizeY; y++) {
            for (int x = 0; x < boardSizeX; x++) {
                CreateBoardItem(x, y);
            }
        }

        // Show possible move.
        ArrayList possibleMoves = match3.GetPossibleMoves();
        Match3.Point p = possibleMoves[Random.Range(0, possibleMoves.Count)] as Match3.Point;
        ShowPossibleMove(p.x, p.y);

        gameState = GameState.Idle;
    }

    /// <summary>
    /// Instantiates and sets up new board item.
    /// </summary>
    /// <returns>Reference to the new board item.</returns>
    /// <param name="x">X-coord.</param>
    /// <param name="y">Y-coord.</param>
    /// <param name="posAdjustX">Adjust of real x-position - for drop animation.</param>
    /// <param name="posAdjustY">Adjust of real y-position - for drop animation.</param>
    BoardItem CreateBoardItem(int x, int y, int posAdjustX = 0, int posAdjustY = 0) {
        // Instantiate item.
        BoardItem clone = Instantiate(boardItems[match3.Board[x, y].type], GetPosOnBoard(x + posAdjustX, y + posAdjustY),
                                      Quaternion.identity) as BoardItem;
        clone.transform.parent = transform;

        // Save item position for picking.
        clone.X = x;
        clone.Y = y;
        clone.name = "BoardItem " + x.ToString() + "," + y.ToString();

        // Darken bonus item.
        if (match3.Board[x, y].isBonus) {
            Color c = clone.renderer.material.color;
            float darkenAmount = 0.5f;
            clone.renderer.material.color = new Color(c.r * darkenAmount,
                                                      c.g * darkenAmount,
                                                      c.b * darkenAmount,
                                                      c.a);
        }

        // Save item reference into a 2-dim array for fast lookup.
        boardAll[x, y] = clone;

        return clone;
    }

    /// <summary>
    /// Gets the real position of an item on the board based on its coords (indices).
    /// </summary>
    /// <returns>The real position of an item on the board.</returns>
    /// <param name="x">X-coord of an item on the board.</param>
    /// <param name="y">Y-coord of an item on the board.</param>
    Vector3 GetPosOnBoard(int x, int y) {
        return new Vector3(-boardCenter.x + itemSize.x / 2.0f + x * (itemSize.x + itemSpacing.x),
                           -boardCenter.y + itemSize.y / 2.0f + y * (itemSize.y + itemSpacing.y), 0.0f);
    }

    /// <summary>
    /// Shows the possible move.
    /// </summary>
    /// <param name="x">X-coord of the possible move.</param>
    /// <param name="y">Y-coord of the possible move.</param>
    void ShowPossibleMove(int x, int y) {
        if (possibleHighlightObj == null) {
            possibleHighlightObj = Instantiate(possibleMoveHighlight, GetPosOnBoard(x, y), Quaternion.identity) as GameObject;
            possibleHighlightObj.transform.parent = transform;
        }
        else {
            possibleHighlightObj.renderer.enabled = true;
            possibleHighlightObj.transform.position = GetPosOnBoard(x, y);
        }
    }

    /// <summary>
    /// Called on touch began/mouse down; selects board item.
    /// </summary>
    /// <param name='position'>Touch/mouse position.</param>
    void MyMouseDown(Vector2 position) {
        RaycastHit hitInfo = new RaycastHit();
        if (Physics.Raycast(Camera.main.ScreenPointToRay(position), out hitInfo)) {
            // Save selected object.
            selObj1 = hitInfo.transform.gameObject.GetComponent(typeof(BoardItem)) as BoardItem;

            // Null if it is not a BoardItem.
            if (selObj1) {
                selObj1.renderer.material.color *= 1.5f;
                dragOrigin = position;
            }
        }
        else {
            selObj1 = null;
        }
    }

    /// <summary>
    /// Called on touch/mouse moved; performs swap of board items.
    /// </summary>
    /// <param name='position'>Touch/mouse position.</param>
    void MyMouseMove(Vector2 position) {
        if (!selObj1)
            return;

        Vector2 dragVec = position - dragOrigin;

        if (dragVec.sqrMagnitude >= dragLength * dragLength) {
            // Drag detected, perform item swap.
            int x1 = selObj1.X;
            int y1 = selObj1.Y;

            int x2 = x1;
            int y2 = y1;

            DragDirection dir = GetDragDirFromVec(dragVec);

            switch (dir) {
                case DragDirection.Left:
                    x2--;
                    break;

                case DragDirection.Right:
                    x2++;
                    break;

                case DragDirection.Up:
                    y2++;
                    break;

                case DragDirection.Down:
                    y2--;
                    break;
            }

            if (x2 >= 0 && x2 < boardSizeX && y2 >= 0 && y2 < boardSizeY) {
                selObj2 = boardAll[x2, y2];

                // Start swap animation.
                selObj1.AnimateMove(selObj2.transform.position, swapAnimSpeed);
                selObj2.AnimateMove(selObj1.transform.position, swapAnimSpeed);

                gameState = GameState.MoveCheck;
            }

            selObj1.ResetColor();
        }
    }

    /// <summary>
    /// Called on touch ended/mouse up; resets first selected board item.
    /// </summary>
    /// <param name='position'>Touch/mouse position.</param>
    void MyMouseUp(Vector2 position) {
        if (selObj1) {
            selObj1.ResetColor();
            selObj1 = null;
        }
    }

    /// <summary>
    /// Gets drag direction from a vector.
    /// </summary>
    /// <returns>Drag direction.</returns>
    /// <param name='dragVec'>Drag vector.</param>
    DragDirection GetDragDirFromVec(Vector2 dragVec) {
        float angle = Vector2.Angle(Vector2.right, dragVec);

        if (angle > 135.0f) {
            return DragDirection.Left;
        }
        else if (angle > 45.0f) {
            if (dragVec.y > 0) {
                return DragDirection.Up;
            }
            else {
                return DragDirection.Down;
            }
        }
        else {
            return DragDirection.Right;
        }
    }

    /// <summary>
    /// Swaps board items - coords, positions, names and updates the boardAll array.
    /// </summary>
    /// <param name='f'>First item.</param>
    /// <param name='s'>Second item.</param>  
    /// <param name='positions'>If <c>false</c>, real positions are not swapped.</param>  
    void SwapObjects(BoardItem f, BoardItem s, bool positions = true) {
        // Swap coord properties (position on the board).
        int tmpX = f.X; f.X = s.X; s.X = tmpX;
        int tmpY = f.Y; f.Y = s.Y; s.Y = tmpY;

        // Swap positions (real position in the world).
        if (positions) {
            Vector3 pos = f.transform.position;
            f.transform.position = s.transform.position;
            s.transform.position = pos;
        }

        // Swap names.
        string name = f.transform.name;
        f.transform.name = s.transform.name;
        s.transform.name = name;

        // Swap references in array.
        BoardItem bi = boardAll[f.X, f.Y];
        boardAll[f.X, f.Y] = boardAll[s.X, s.Y];
        boardAll[s.X, s.Y] = bi;
    }
}
