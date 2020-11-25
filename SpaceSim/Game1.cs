using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

[assembly: SpriteGenerator.SpriteGeneratorNamespace("SpaceSim.Graphics")]

namespace SpaceSim
{

    
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        private readonly MonoGame.Extended.Screens.ScreenManager _screenManager;

        public SpriteBatch SpriteBatch { get; private set; }

        public Game1()
        {
            this._graphics = new GraphicsDeviceManager(this);
            this.Content.RootDirectory = "Content";
            this.IsMouseVisible = true;
            this._screenManager = new MonoGame.Extended.Screens.ScreenManager();
            this.Components.Add(this._screenManager);
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
            //_screenManager.LoadScreen(new PlayingScreen(this), new MonoGame.Extended.Screens.Transitions.FadeTransition(GraphicsDevice, Color.Black));
            this._screenManager.LoadScreen(new SelectScreen(this), new MonoGame.Extended.Screens.Transitions.FadeTransition(this.GraphicsDevice, Color.Black));
        }

        public void GoToGame(int playerIndex)
        {
            this._screenManager.LoadScreen(new PlayingScreen(this, playerIndex), new MonoGame.Extended.Screens.Transitions.FadeTransition(this.GraphicsDevice, Color.Black));
        }
        public void GoToCharacterSelection()
        {
            this._screenManager.LoadScreen(new SelectScreen(this), new MonoGame.Extended.Screens.Transitions.FadeTransition(this.GraphicsDevice, Color.Black));
        }

        protected override void LoadContent()
        {
            this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            // TODO: use this.Content to load your game content here

            

        }

        protected override void Update(GameTime gameTime)
        {
            var state = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || state.IsKeyDown(Keys.F12))
                this._graphics.ToggleFullScreen();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}

