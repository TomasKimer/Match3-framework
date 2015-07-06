using UnityEngine;
using System.Collections;

/// <summary>
/// Match 3 game logic (model).
/// </summary>
public class Match3 {

    /// <summary>
    /// Represents an integer 2d point.
    /// </summary>
    public class Point {
        public int x;
        public int y;

        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// Represents one board item.
    /// </summary>
    public class BoardItem {

        /// <summary>
        /// Item ID.
        /// </summary>
        public int type;

        /// <summary>
        /// If the item has been destroyed.
        /// </summary>
        public bool destroyed = false;

        /// <summary>
        /// Item is a bonus.
        /// </summary>
        public bool isBonus = false;

        /// <summary>
        /// The bonus info.
        /// </summary>
        public BonusInfo bonusInfo;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="type">Type.</param>
        public BoardItem(int type) {
            this.type = type;
        }

        /// <summary>
        /// Initializes a new instance with random generation of an item ID
        /// and with bonus probability of 2%.
        /// </summary>
        /// <param name="minType">Minimum item ID.</param>
        /// <param name="maxType">Maximum item ID.</param>
        /// <param name="bonusProbability">Probability of the bonus.</param>
        public BoardItem(int minType, int maxType, float bonusProbability = 0.02f) {
            GenerateNew(minType, maxType, bonusProbability);
        }

        /// <summary>
        /// Generates the random item ID and bonus info.
        /// </summary>
        /// <param name="minType">Minimum item ID.</param>
        /// <param name="maxType">Maximum item ID.</param>
        /// <param name="bonusProbability">Probability of the bonus.</param>
        public void GenerateNew(int min, int max, float bonusProbability = 0.02f) {
            type = Random.Range(min, max);

            isBonus = Random.value > (1.0f - bonusProbability);
            if (isBonus) {
                bonusInfo = new BonusInfo(BonusInfo.BonusShape.Cross, 3, 3); // 3 from each side (12 in total)
            }
        }
    }

    /// <summary>
    /// Bonus info (shape etc.).
    /// </summary>
    public class BonusInfo {

        /// <summary>
        /// Posible bonus shapes.
        /// </summary>
        public enum BonusShape {
            Cross
        }

        /// <summary>
        /// Current bonus shape.
        /// </summary>
        public BonusShape bonusShape;

        /// <summary>
        /// Item count of a bonus shape in the x-axis (from each side).
        /// </summary>
        public int itemCountX;

        /// <summary>
        /// Item count of a bonus shape in the y-axis (from each side).
        /// </summary>
        public int itemCountY;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="bonusShape">Bonus shape.</param>
        /// <param name="itemCountX">Item count in the x-axis (from each side).</param>
        /// <param name="itemCountY">Item count in the y-axis (from each side).</param>
        public BonusInfo(BonusShape bonusShape, int itemCountX, int itemCountY) {
            this.bonusShape = bonusShape;
            this.itemCountX = itemCountX;
            this.itemCountY = itemCountY;
        }

        /// <summary>
        /// Destroys items in a given shape.
        /// </summary>
        /// <returns>Points of destroyed items.</returns>
        /// <param name="refBoard">Reference to the game board.</param>
        /// <param name="centerX">X-coord of the shape centre.</param>
        /// <param name="centerY">Y-coord of the shape centre.</param>
        public ArrayList ApplyBonus(BoardItem[,] refBoard, int centerX, int centerY) {
            switch (bonusShape) {

                case BonusShape.Cross:
                    return DestroyItemsInCrossShape(refBoard, centerX, centerY);
            }
            return null;
        }

        /// <summary>
        /// Destroys items in a cross shape.
        /// </summary>
        /// <returns>Points of destroyed items.</returns>
        /// <param name="refBoard">Reference to the game board.</param>
        /// <param name="centerX">X-coord of the shape centre.</param>
        /// <param name="centerY">Y-coord of the shape centre.</param>
        public ArrayList DestroyItemsInCrossShape(BoardItem[,] refBoard, int centerX, int centerY) {
            ArrayList destroyed = new ArrayList();

            // X-line.
            for (int x = centerX - itemCountX; x <= centerX + itemCountX; x++) {
                if (x >= 0 && x < refBoard.GetLength(0)) {
                    if (!refBoard[x, centerY].destroyed) {
                        refBoard[x, centerY].destroyed = true;
                        destroyed.Add(new Point(x, centerY));
                    }
                }
            }
            // Y-line.
            for (int y = centerY - itemCountY; y <= centerY + itemCountY; y++) {
                if (y >= 0 && y < refBoard.GetLength(1)) {
                    if (!refBoard[centerX, y].destroyed) {
                        refBoard[centerX, y].destroyed = true;
                        destroyed.Add(new Point(centerX, y));
                    }
                }
            }

            return destroyed;
        }
    }

    /// <summary>
    /// Possible states of the game.
    /// TODO Use for assertions etc.
    /// </summary>
    public enum GameState {
        Idle,
        GamePlaying,
        GameOver
    }

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    /// <value>The game state.</value>
    public GameState State {
        get { return gameState; }
    }

    /// <summary>
    /// Gets the board.
    /// </summary>
    /// <value>The board.</value>
    public BoardItem[,] Board {
        get { return board; }
    }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    /// <value>The score.</value>
    public int Score {
        get { return score; }
    }


    /// <summary>
    /// The board.
    /// </summary>
    private BoardItem[,] board;

    /// <summary>
    /// Board width.
    /// </summary>
    private int boardSizeX;

    /// <summary>
    /// Board height.
    /// </summary>
    private int boardSizeY;

    /// <summary>
    /// Number of various items on the board.
    /// </summary>
    private int variousItemCount;

    /// <summary>
    /// Current score value.
    /// </summary>
    private int score;

    /// <summary>
    /// The score multiplier.
    /// </summary>
    private int scoreMult = 50;

    /// <summary>
    /// Current state of the game.
    /// </summary>
    private GameState gameState = GameState.Idle;


    /// <summary>
    /// Makes new board and initializes new game.
    /// </summary>
    /// <param name='boardSizeX'>Board width.</param>
    /// <param name='boardSizeY'>Board height.</param>
    /// <param name='variousItemCount'>Various item count.</param>
    public void MakeNewBoard(int boardSizeX, int boardSizeY, int variousItemCount) {
        board = new BoardItem[boardSizeX, boardSizeY];
        this.boardSizeX = boardSizeX;
        this.boardSizeY = boardSizeY;
        this.variousItemCount = variousItemCount;
        score = 0;

        int generateCount = 0;

        // Valid board generation.
        while (true) {
            // Fill the board with random items.
            for (int y = 0; y < boardSizeY; y++) {
                for (int x = 0; x < boardSizeX; x++) {
                    board[x, y] = new BoardItem(0, variousItemCount);
                }
            }

            generateCount++;

            // Check for existing matches.
            if (LookForMatches(false).Count > 0) {
                continue;
            }

            // Check if there is at least one possible move for the player.
            ArrayList possibles = LookForPossibles(false);
            if (possibles.Count == 0) {
                continue;
            }

            // Suitable board found.
            break;
        }
        Debug.Log("Board re-generated: " + (generateCount - 1).ToString() + "x");

        gameState = GameState.GamePlaying;
    }

    /// <summary>
    /// Checks if the swap is valid.
    /// </summary>
    /// <returns><c>true</c> if the swap is valid.</returns>
    /// <param name='x1'>X coord of the first board item.</param>
    /// <param name='y1'>Y coord of the first board item.</param>
    /// <param name='x2'>X coord of the second board item.</param>
    /// <param name='y2'>Y coord of the second board item.</param>
    public bool CheckMove(int x1, int y1, int x2, int y2) {
        SwapItems(x1, y1, x2, y2);

        if (LookForMatches().Count > 0) {
            return true;
        }
        SwapItems(x1, y1, x2, y2);

        return false;
    }

    /// <summary>
    /// Gets the matches.
    /// </summary>
    /// <returns>The matches.</returns>
    public ArrayList GetMatches() {
        ArrayList matches = LookForMatches();

        // Remove all existing matches and update the score.
        foreach (ArrayList match in matches) {
            score += match.Count * scoreMult;
            foreach (Point p in match) {
                board[p.x, p.y].destroyed = true;
            }
        }
        // Apply bonuses.
        for (int y = 0; y < boardSizeY; y++) {
            for (int x = 0; x < boardSizeX; x++) {
                BoardItem bi = board[x, y];
                if (bi.isBonus && bi.destroyed) {
                    // Bonus was triggered, apply shape.
                    ArrayList bonusMatch = bi.bonusInfo.ApplyBonus(board, x, y);
                    // Shape has destroyed some items, update the score and save destroyed positions.
                    if (bonusMatch.Count > 0) {
                        score += bonusMatch.Count * scoreMult;
                        matches.Add(bonusMatch);
                    }
                }
            }
        }
        return matches;
    }

    /// <summary>
    /// Performs drop of existing items and returns positions of these swaps.
    /// </summary>
    /// <returns>Array of swaps (Point[2]). If all these items are
    /// swapped, complete drop is done.</returns>
    public ArrayList GetDropSwaps() {
        ArrayList swaps = new ArrayList();

        // From left to right.
        for (int x = 0; x < boardSizeX; x++) {
            ArrayList destroyed = new ArrayList();
            // From down to bottom.
            for (int y = 0; y < boardSizeY; y++) {
                if (board[x, y].destroyed) {
                    // Save destroyed position.
                    destroyed.Add(new Point(x, y));
                }
                // Current pos was not destroyed and there are some destroyed ones below.
                else if (destroyed.Count > 0) {
                    // Get the first destroyed position.
                    Point p = destroyed[0] as Point;

                    // Swap positions.
                    SwapItems(x, y, p.x, p.y);
                    swaps.Add(new Point[] { new Point(x, y), p });

                    // Remove previous destroyed position and add current.
                    destroyed.RemoveAt(0);
                    destroyed.Add(new Point(x, y));
                }
            }
        }
        return swaps;
    }

    /// <summary>
    /// Gets coords of destroyed items (already dropped) and generates new ones.
    /// </summary>
    /// <returns>Points of destroyed items.</returns>
    public ArrayList GetDestroyedItemsAndGenerateNew() {
        ArrayList destroyed = new ArrayList();

        for (int x = 0; x < boardSizeX; x++) {
            for (int y = 0; y < boardSizeY; y++) {
                if (board[x, y].destroyed) {
                    destroyed.Add(new Point(x, y));
                    board[x, y].destroyed = false;
                    board[x, y].GenerateNew(0, variousItemCount);
                }
            }
        }
        return destroyed;
    }

    /// <summary>
    /// Gets the possible moves.
    /// </summary>
    /// <returns>Array of points of possible moves.</returns>
    public ArrayList GetPossibleMoves() {
        ArrayList possibleMoves = LookForPossibles(true);

        if (possibleMoves.Count == 0) {
            gameState = GameState.GameOver;
        }
        return possibleMoves;
    }


    /// <summary>
    /// Gets matches from board.
    /// </summary>
    /// <returns>Array of arrays of Points in match.</returns>
    /// <param name='all'>If false, stops at the first match.</param>
    private ArrayList LookForMatches(bool all = true) {
        ArrayList matches = new ArrayList();

        // Search for horizontal matches.
        for (int y = 0; y < boardSizeY; y++) {
            for (int x = 0; x < boardSizeX - 2; x++) {
                ArrayList matchRun = GetMatchRunX(x, y);
                if (matchRun.Count >= 3) {
                    // Horizontal match found.
                    matches.Add(matchRun);
                    if (!all) {
                        return matches;
                    }
                    x += matchRun.Count - 1;
                }
            }
        }

        // Search for vertical matches.
        for (int x = 0; x < boardSizeX; x++) {
            for (int y = 0; y < boardSizeY - 2; y++) {
                ArrayList matchRun = GetMatchRunY(x, y);
                if (matchRun.Count >= 3) {
                    // Vertical match found.
                    matches.Add(matchRun);
                    if (!all) {
                        return matches;
                    }
                    y += matchRun.Count - 1;
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Gets all Points in horizontal (x, from left to right) match.
    /// </summary>
    /// <returns>Array of Points in horizontal match.</returns>
    /// <param name='x'>X-coord of start position.</param>
    /// <param name='y'>Y-coord of start position.</param>
    private ArrayList GetMatchRunX(int x, int y) {
        ArrayList matchRun = new ArrayList();
        matchRun.Add(new Point(x, y));

        for (int i = x; i < boardSizeX - 1; i++) {
            if (board[i, y].type == board[i + 1, y].type) {
                matchRun.Add(new Point(i + 1, y));
            }
            else {
                break;
            }
        }
        return matchRun;
    }

    /// <summary>
    /// Gets all Points in vertical (y, from down to up) match.
    /// </summary>
    /// <returns>Array of Points in vertical match.</returns>
    /// <param name='x'>X-coord of start position.</param>
    /// <param name='y'>Y-coord of start position.</param>
    private ArrayList GetMatchRunY(int x, int y) {
        ArrayList matchRun = new ArrayList();
        matchRun.Add(new Point(x, y));

        for (int i = y; i < boardSizeY - 1; i++) {
            if (board[x, i].type == board[x, i + 1].type) {
                matchRun.Add(new Point(x, i + 1));
            }
            else {
                break;
            }
        }
        return matchRun;
    }

    /// <summary>
    /// Swaps two items on the board.
    /// </summary>
    /// <param name='x1'>X-coord of the first item.</param>
    /// <param name='y1'>Y-coord of the first item.</param>
    /// <param name='x2'>X-coord of the second item.</param>
    /// <param name='y2'>Y-coord of the second item.</param>
    private void SwapItems(int x1, int y1, int x2, int y2) {
        BoardItem tmp = board[x1, y1];
        board[x1, y1] = board[x2, y2];
        board[x2, y2] = tmp;
    }

    /// <summary>
    /// Looks for possible swaps.
    /// </summary>
    /// <returns>Array of Points which, if swapped, could make a match.</returns>
    /// <param name='all'>If false, returns only the first match.</param>
    private ArrayList LookForPossibles(bool all = true) {

        // Patterns of relative coordinates to 0,0.
        // First 2-dim array in ArrayList = must-haves, second = need-ones.
        ArrayList patterns = new ArrayList();

        // First pattern (left or right edge).
        patterns.Add(new int[,] { { 1, 0 } });
        patterns.Add(new int[,] { { -2, 0 }, { -1, -1 }, { -1, 1 }, { 2, -1 }, { 2, 1 }, { 3, 0 } });

        // First pattern (top or bottom edge).
        patterns.Add(new int[,] { { 0, 1 } });
        patterns.Add(new int[,] { { 0, -2 }, { -1, -1 }, { 1, -1 }, { -1, 2 }, { 1, 2 }, { 0, 3 } });

        // Second pattern (middle horizontal).
        patterns.Add(new int[,] { { 2, 0 } });
        patterns.Add(new int[,] { { 1, -1 }, { 1, 1 } });

        // Second pattern (middle vertical).
        patterns.Add(new int[,] { { 0, 2 } });
        patterns.Add(new int[,] { { -1, 1 }, { 1, 1 } });


        ArrayList possibles = new ArrayList();

        // For all items on the board...
        for (int y = 0; y < boardSizeY; y++) {
            for (int x = 0; x < boardSizeX; x++) {
                // ...tries to match all of patterns.
                for (int i = 0; i < patterns.Count / 2; i++) {
                    Point p = MatchPattern(x, y, patterns[i * 2] as int[,], patterns[i * 2 + 1] as int[,]);
                    if (p != null) {
                        // Pattern matched, save position of possible swap.
                        possibles.Add(p);
                        if (!all) {
                            return possibles;
                        }
                    }
                }
            }
        }

        return possibles;
    }

    /// <summary>
    /// Tries to match a pattern.
    /// </summary>
    /// <returns>Coords of an item, that can be swapped, or null.</returns>
    /// <param name='x'>X-coord of current checking position on board.</param>
    /// <param name='y'>Y-coord of current checking position on board.</param>
    /// <param name='mustHave'>Array of coords that must be all of the same type as the current item on board.</param>
    /// <param name='needOne'>Same as above, but only one point need to be the same.</param>
    private Point MatchPattern(int x, int y, int[,] mustHave, int[,] needOne) {
        int type = board[x, y].type;

        // Make sure that current item has all must-haves.
        for (int i = 0; i < mustHave.GetLength(0); i++) {
            if (!MatchType(x + mustHave[i, 0], y + mustHave[i, 1], type)) {
                return null;
            }
        }

        // Make sure that current item has at least one need-ones.
        for (int i = 0; i < needOne.GetLength(0); i++) {
            if (MatchType(x + needOne[i, 0], y + needOne[i, 1], type)) {
                return new Point(x + needOne[i, 0], y + needOne[i, 1]);
            }
        }

        return null;
    }

    /// <summary>
    /// Matches the type with item on board.
    /// </summary>
    /// <returns>True if the types matched.</returns>
    /// <param name='x'>X-coord of the item on board.</param>
    /// <param name='y'>Y-coord of the item on board.</param>
    /// <param name='type'>Type to match.</param>
    private bool MatchType(int x, int y, int type) {
        // Coords must be within the board...
        if (x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY) {
            // ...and the type must be the same.
            return board[x, y].type == type;
        }
        return false;
    }
}
