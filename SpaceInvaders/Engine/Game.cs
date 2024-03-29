﻿using SpaceInvaders.Controllers;
using SpaceInvaders.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;

namespace SpaceInvaders.Engine
{
    /// <summary>
    /// GameState enum
    /// </summary>
    public enum GameState
    {
        Start,
        Play,
        Pause,
        Win,
        Lost
    }

    /// <summary>
    /// This class represents the entire game, it implements the singleton pattern
    /// </summary>
    internal class Game
    {
        #region Static Fields

        /// <summary>
        /// Singleton for easy access
        /// </summary>
        public static Game GameInstance { get; private set; }

        /// <summary>
        /// A shared black brush
        /// </summary>
        public static readonly System.Drawing.Brush BlackBrush = new SolidBrush(System.Drawing.Color.Black);

        /// <summary>
        /// A shared simple font
        /// </summary>
        public static readonly Font DefaultFont = new Font("Times New Roman", 24, FontStyle.Bold, GraphicsUnit.Pixel);

        #endregion

        #region GameObjects Management

        /// <summary>
        /// Set of all game objects currently in the game
        /// </summary>
        public HashSet<GameObject> GameObjects { get; private set; }

        /// <summary>
        /// Set of new game objects scheduled for addition to the game
        /// </summary>
        private HashSet<GameObject> pendingNewGameObjects = new HashSet<GameObject>();

        /// <summary>
        /// Schedule a new object for addition in the game.
        /// The new object will be added at the beginning of the next update loop
        /// </summary>
        /// <param name="gameObject">object to add</param>
        public void AddNewGameObject(GameObject gameObject)
        {
            pendingNewGameObjects.Add(gameObject);
        }

        #endregion

        #region Game Technical Elements

        /// <summary>
        /// Size of the game area
        /// </summary>
        public Size GameSize { get; private set; }

        /// <summary>
        /// State of the keyboard
        /// </summary>
        public readonly HashSet<Keys> KeyPressed = new HashSet<Keys>();

        /// <summary>
        /// Current state of the game
        /// </summary>
        private GameState gameState;

        /// <summary>
        /// Theme sound of the game
        /// </summary>
        private readonly MediaPlayer themePlayer = Sound.Theme;

        #endregion

        #region Game Physical Elements

        /// <summary>
        /// Player spaceship
        /// </summary>
        public PlayerSpaceShip PlayerShip { get; private set; }

        /// <summary>
        /// Block of enemies moving on the screen
        /// </summary>
        private EnemyBlock enemyBlock;

        #endregion

        #region Constructors

        /// <summary>
        /// Singleton constructor
        /// </summary>
        /// <param name="gameSize">Size of the game area</param>
        /// 
        /// <returns>instance of the game</returns>
        public static Game CreateGame(Size gameSize)
        {
            return GameInstance ?? (GameInstance = new Game(gameSize));
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        /// <param name="gameSize">Size of the game area</param>
        private Game(Size gameSize)
        {
            GameSize = gameSize;
            GameObjects = new HashSet<GameObject>();
            pendingNewGameObjects = new HashSet<GameObject>();
        }

        #endregion

        #region Live Game Methods

        /// <summary>
        /// Draw the whole game
        /// </summary>
        /// <param name="g">Graphics to draw in</param>
        public void Draw(Graphics g)
        {
            DrawGameMessage(g);
            foreach (var gameObject in GameObjects)
                gameObject.Draw(this, g);
        }

        /// <summary>
        /// Update game
        /// </summary>
        /// <param name="deltaT">Elapsed time since last frame</param>
        public void Update(double deltaT)
        {
            HandleGameState(out var update);
            if (!update) return;
            // add new game objects
            GameObjects.UnionWith(pendingNewGameObjects);
            pendingNewGameObjects.Clear();

            // update each game object
            foreach (var gameObject in GameObjects)
                gameObject.Update(this, deltaT);

            // remove dead objects
            GameObjects.RemoveWhere(gameObject => !gameObject.IsAlive());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Force a given key to be ignored in following updates until the user
        /// explicitly retype it or the system auto fires it again.
        /// </summary>
        /// <param name="key">key to ignore</param>
        public void ReleaseKey(Keys key)
        {
            KeyPressed.Remove(key);
        }

        /// <summary>
        /// Spawn the player spaceship at the middle of the screen
        /// </summary>
        private void SpawnPlayer()
        {
            var position = new Vector2(GameSize.Width * .5f, GameSize.Height * .9f);
            PlayerShip = new PlayerSpaceShip(100, position, 3);
            AddNewGameObject(PlayerShip);
        }

        /// <summary>
        /// Spawn 3 bunkers
        /// </summary>
        private void SpawnBunkers()
        {
            var split = GameSize.Width / 3;
            var bunkerWidth = Properties.Resources.bunker.Width;
            for (var i = 0; i < 3; i++)
            {
                var position = new Vector2(split * (i + .5f) - bunkerWidth * .5f, GameSize.Height * .75);
                GameObject bunker = new Bunker(position);
                AddNewGameObject(bunker);
            }
        }

        /// <summary>
        /// Spawn the enemy block on the top left corner
        /// </summary>
        private void SpawnEnemyBlock()
        {
            enemyBlock = new EnemyBlock(new Vector2());
            AddNewGameObject(enemyBlock);
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        private void NewGame()
        {
            Score.UpdateLevel(gameState == GameState.Lost);

            GameObjects = new HashSet<GameObject>();
            pendingNewGameObjects = new HashSet<GameObject>();

            SpawnPlayer();
            SpawnBunkers();
            SpawnEnemyBlock();

            themePlayer.Stop();
            themePlayer.Play();

            gameState = GameState.Play;
        }

        /// <summary>
        /// Switch between play pause when P is pressed
        /// </summary>
        /// /// <param name="nextState">next game state when P will be pressed</param>
        private void HandlePlayPause(GameState nextState)
        {
            if (!KeyPressed.Contains(Keys.P)) return;
            ReleaseKey(Keys.P);
            gameState = nextState;
            if (nextState == GameState.Pause)
                themePlayer.Pause();
            else
                themePlayer.Play();
        }

        /// <summary>
        /// Switch between win and lose depending on the situation
        /// </summary>
        private void HandleWinLoss()
        {
            if (enemyBlock != null && !enemyBlock.IsAlive())
                gameState = GameState.Win;
            else if (!PlayerShip.IsAlive())
            {
                Score.Save();
                gameState = GameState.Lost;
            }
        }

        /// <summary>
        /// Switch game state and handle actions depending on it
        /// </summary>
        private void HandleGameState(out bool update)
        {
            update = false;
            switch (gameState)
            {
                case GameState.Play:
                    update = true;
                    HandlePlayPause(GameState.Pause);
                    HandleWinLoss();
                    break;
                case GameState.Pause:
                    HandlePlayPause(GameState.Play);
                    break;
                case GameState.Start:
                case GameState.Win:
                case GameState.Lost:
                    if (KeyPressed.Contains(Keys.Space))
                    {
                        ReleaseKey(Keys.Space);
                        NewGame();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void DrawGameMessage(Graphics g)
        {
            var text = "";
            SizeF textSize;
            float positionX = 0;
            float positionY = 0;

            switch (gameState)
            {
                case GameState.Start:
                    text = "PRESS SPACE TO START";
                    textSize = g.MeasureString(text, DefaultFont);
                    positionX = GameSize.Width * .5f - textSize.Width * .5f;
                    positionY = GameSize.Height * .5f - textSize.Height * .5f;
                    break;
                case GameState.Play:
                    g.DrawString(Score.ToString(), DefaultFont, BlackBrush, 0, 0);
                    break;
                case GameState.Pause:
                    text = "PAUSED";
                    textSize = g.MeasureString(text, DefaultFont);
                    positionX = GameSize.Width * .5f - textSize.Width * .5f;
                    positionY = GameSize.Height * .5f - textSize.Height * .5f;
                    g.DrawString(Score.ToString(), DefaultFont, BlackBrush, 0, 0);
                    break;
                case GameState.Win:
                    text = "PRESS SPACE TO CONTINUE";
                    textSize = g.MeasureString(text, DefaultFont);
                    positionX = GameSize.Width * .5f - textSize.Width * .5f;
                    positionY = GameSize.Height * .5f - textSize.Height * .5f;
                    break;
                case GameState.Lost:
                    g.DrawString(Score.ScoreBoard(), DefaultFont, BlackBrush, 0, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            g.DrawString(text, DefaultFont, BlackBrush, positionX, positionY);
        }

        #endregion
    }
}