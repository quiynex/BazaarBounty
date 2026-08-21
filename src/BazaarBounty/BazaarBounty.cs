using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ViewportAdapters;


namespace BazaarBounty
{
    public class BazaarBountyGame : Game
    {
        private static BazaarBountyGame gameInstance = null;

        public static BazaarBountyGame GetGameInstance()
        {
            gameInstance ??= new();
            return gameInstance;
        } 

        public static GameTime GetGameTime()
        {
            return gameInstance._gameTime;
        }

        public void ToggleFullScreen() {
            _graphics.ToggleFullScreen();
        }

        public enum GameState
        {
            Starting,
            Running,
            Pausing,
            Ending
        }

        #region GameStateManagementFunctions

        public GameState CurrentGameState
        {
            get { return currentGameState; }
            set { currentGameState = value; }
        }
        public void ResetGame(){
            CurrentGameState = GameState.Starting;
            // Play Main Theme
            MediaPlayer.Play(musicManager.mainTheme);
        }
        public void StartNewGame() {
            Console.WriteLine("StartNewGame()");

            // Create Collision Component
            _collisionComponent = new(new RectangleF(0, 0, Settings.Graphics.ScreenWidth + 1000, Settings.Graphics.ScreenHeight + 1000));
            // Set GameState
            CurrentGameState = GameState.Running;

            // Initialize Player and PlayerController
            playerController = new();
            playerController.RegisterControllerInput();
            player = new PlayerCharacter(new Vector2(0, 0));
            player.LoadContent(); // Additionally LoadContent for Player (Enemy LoadContent is handled by LevelManager)
            playerController.AddCharacter(player);
            _collisionComponent.Insert(player);
            ((MeleeWeapon)player.PrimaryWeapon).RegisterHitBox(_collisionComponent);

            // Initialize new LevelManager
            levelManager = new LevelManager();
            // LevelManager handles Maps, Enemies and Fruits
            levelManager.LoadNextMap();

            //Initialize music manager
            musicManager.Initialize();

            // Add in-game Status Bar
            uiManager.AddWidget(uiManager.StatusBar);
        }

        public void PauseGame() {
            CurrentGameState = GameState.Pausing;
            playerController.ClearControllerInput();
            uiManager.RemoveWidget(uiManager.StatusBar);
            MediaPlayer.Pause();
        }

        public void PowerUpSelection(List<Buff> buffs) {
            // pause the game w/o hiding the status bar
            CurrentGameState = GameState.Pausing;
            playerController.ClearControllerInput();
            var fruitsWidget = new FruitsWidget(buffs);
            uiManager.AddWidget(fruitsWidget);
        }

        public void ContinueGame()
        {
            CurrentGameState = GameState.Running;
            playerController.RegisterControllerInput();
            uiManager.AddWidget(uiManager.StatusBar);
            MediaPlayer.Resume();
        }

        public void EndGame() {
            MediaPlayer.Play(musicManager.defeat_bgm);
            Console.WriteLine("Ending Game");
            CurrentGameState = GameState.Ending;
            playerController.ClearControllerInput();
            player.ResetProperty();
            uiManager.AddWidget(uiManager.EndWidget);
            uiManager.RemoveWidget(uiManager.StatusBar);
            levelManager.EnemyController.ClearCharacters();
            NavigationSystem.ResetNavigationInfo();
            levelManager.CurrentMap.Unload(_collisionComponent);
            levelManager.DespawnFruits(_collisionComponent);
            levelManager.ResetManager();
            _collisionComponent = new(new RectangleF(0, 0, Settings.Graphics.ScreenWidth, Settings.Graphics.ScreenHeight));

        }

        public void WinGame() {
            Console.WriteLine("Won Game");
            MediaPlayer.Stop();
            musicManager.triumph_sound.Play();
            CurrentGameState = GameState.Ending;
            playerController.ClearControllerInput();
            player.ResetProperty();
            uiManager.AddWidget(uiManager.WinWidget);
            uiManager.RemoveWidget(uiManager.StatusBar);
            levelManager.EnemyController.ClearCharacters();
            NavigationSystem.ResetNavigationInfo();
            levelManager.CurrentMap.Unload(_collisionComponent);
            levelManager.DespawnFruits(_collisionComponent);
            levelManager.ResetManager();
            _collisionComponent = new(new RectangleF(0, 0, Settings.Graphics.ScreenWidth, Settings.Graphics.ScreenHeight));

        }

        #endregion


        public GraphicsDeviceManager _graphics; // set public for now
        private GameTime _gameTime;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _uiSpriteBatch;
        private CollisionComponent _collisionComponent;
        public CollisionComponent CollisionComp => _collisionComponent;

        public static PlayerCharacter player;
        public static PlayerController playerController; 
        public static UserInterfaceManager uiManager; 
        public static LevelManager levelManager;
        public static MusicManager musicManager;
        public static OrthographicCamera camera;
        public static ParticleEffectManager particleEffectManager;
        private GameState currentGameState;
        public static Settings.Root Settings { get; set; }


        private BazaarBountyGame()
        {
            _graphics = new GraphicsDeviceManager(this);

            // Settings
            Settings = SettingsLoader.LoadSettings("settings.json");
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            currentGameState = GameState.Starting;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = Settings.Graphics.ScreenWidth;
            _graphics.PreferredBackBufferHeight = Settings.Graphics.ScreenHeight;
            _graphics.IsFullScreen = Settings.Graphics.Fullscreen;
            _graphics.ApplyChanges();
            
            var viewportAdapter = new BoxingViewportAdapter(Window, _graphics.GraphicsDevice, Settings.Graphics.ScreenWidth, Settings.Graphics.ScreenHeight);
            camera = new OrthographicCamera(viewportAdapter);
            camera.ZoomIn(0);

            uiManager = new();
            musicManager = new();

            // Play Main Theme
            MediaPlayer.Play(musicManager.mainTheme);
            
            particleEffectManager = new ParticleEffectManager(GraphicsDevice);
            ToggleFullScreen();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _uiSpriteBatch = new SpriteBatch(GraphicsDevice);
            particleEffectManager.LoadContent();
            uiManager.LoadContent();
        }

        protected override void Update(GameTime gameTime) 
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            switch (CurrentGameState)
            {
                case GameState.Starting:
                    // Just return
                    break;

                case GameState.Running:

                    // Setup
                    _gameTime = gameTime;
                    player.CharacterGameState.ResetMoveState();

                    // Utils
                    particleEffectManager.Update(gameTime);
                    Utils.TimerPool.GetInstance().Update();
                    ProjectilePool.GetInstance().Update(gameTime);
                    FruitPool.GetInstance().Update(gameTime);
                    playerController.Update(gameTime);

                    // Handle map & camera
                    levelManager.Update(gameTime);
                    levelManager.CurrentMap.ClampCameraToMap(player, camera, 0.8f, 0.8f);
                    _collisionComponent.Update(gameTime);

                    // Handle BGM
                    musicManager.Update(levelManager.MapManager.GetCurrentStage(LevelManager.NextLevelNumber), LevelManager.NextLevelNumber);

                    base.Update(gameTime);

                    // End the game
                    if (player.CurrHealth <= 0) {
                        EndGame();
                        return;
                    }
                    break;

                case GameState.Pausing:
                    break;

                case GameState.Ending:
                    camera.LookAt(new Vector2((float)Settings.Graphics.ScreenWidth/2, (float)Settings.Graphics.ScreenHeight/2));
                    break;
            }
            uiManager.Update();
            InputRouter.HandleInput();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.FrontToBack, transformMatrix: camera.GetViewMatrix());
            switch (CurrentGameState)
            {
                case GameState.Starting:
                    break;

                case GameState.Running:
                case GameState.Pausing:
                    levelManager.Draw(_spriteBatch);
                    player.Draw(_spriteBatch);
                    ProjectilePool.GetInstance().Draw(_spriteBatch);
                    FruitPool.GetInstance().Draw(_spriteBatch);
                    // Debug Draw
                    NavigationSystem.DrawPath(_spriteBatch);
                    particleEffectManager.Draw(_spriteBatch); // Draw before the level (on top)
                    break;

                case GameState.Ending:
                    break;
            }
            _spriteBatch.End();

            _uiSpriteBatch.Begin();
            uiManager.Draw();
            _uiSpriteBatch.End();

            base.Draw(gameTime);
        }

    }
}
