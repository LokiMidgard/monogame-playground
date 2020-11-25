using System;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceSim
{
    public abstract class GameScreen : MonoGame.Extended.Screens.GameScreen
    {
        protected new Game1 Game => (Game1)base.Game;
        public GameScreen(Game1 game) : base(game)
        {
            this.SpriteBatch = game.SpriteBatch;
            
        }
        public override void UnloadContent()
        {
            base.UnloadContent();
            this.Content.Unload();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            GC.Collect();
        }

        protected SpriteBatch SpriteBatch { get; }
    }
}

