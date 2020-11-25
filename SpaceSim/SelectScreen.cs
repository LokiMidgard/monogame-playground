using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using SpaceSim.Graphics;

namespace SpaceSim
{
    public class SelectScreen : GameScreen
    {
        private Animation<Direction4, PlayerAnimation>[] players;

        public SelectScreen(Game1 game) : base(game) { }

        private const int NumberOfColumns = 10;

        private const int NumberOfRows = 10;

        private (int x, int y) selectedChar;

        public override void Update(GameTime gameTime)
        {
            var state = KeyboardExtended.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || state.IsKeyDown(Keys.Escape))
                this.Game.Exit();


            var direction = (Direction4)(int)((gameTime.TotalGameTime.TotalSeconds / 4) % 4);

            foreach (var item in this.players)
            {
                item.Direction = direction;
                item.Update(gameTime);
            }

            if (this.selectedChar.y < NumberOfRows - 1 && state.WasKeyJustDown(Microsoft.Xna.Framework.Input.Keys.Down))
                this.selectedChar.y++;
            if (this.selectedChar.y > 0 && state.WasKeyJustDown(Microsoft.Xna.Framework.Input.Keys.Up))
                this.selectedChar.y--;
            if (this.selectedChar.x < NumberOfRows - 1 && state.WasKeyJustDown(Microsoft.Xna.Framework.Input.Keys.Right))
                this.selectedChar.x++;
            if (this.selectedChar.x > 0 && state.WasKeyJustDown(Microsoft.Xna.Framework.Input.Keys.Left))
                this.selectedChar.x--;

            if (state.WasKeyJustDown(Microsoft.Xna.Framework.Input.Keys.Enter))
                this.Game.GoToGame(this.selectedChar.x + this.selectedChar.y * NumberOfColumns);
        }



        public override void LoadContent()
        {

            Func<Direction4, PlayerAnimation, AnimationSequence<Direction4, PlayerAnimation>> getPlayerSprite(int index)
            {

                return (Direction4 direction, PlayerAnimation animation) =>
                {
                    // width 31 height 36 3 frames
                    const int Width = 32;
                    const int Height = 32;
                    const int numberOfColumns = 10;
                    var baseX = (index % numberOfColumns) * Width * 3;
                    var baseY = (index / numberOfColumns) * Height * 4;
                    const float animationSpeed = 4f;

                    var origin = new Vector2(Width / 2, Height);
                    return (direction, animation) switch
                    {
                        (Direction4.Down, PlayerAnimation.Walking) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + 0, baseY, Width, Height), new Rectangle(baseX + Width, baseY, Width, Height), new Rectangle(baseX + Width * 2, baseY, Width, Height), new Rectangle(baseX + Width, baseY, Width, Height)), animationSpeed, origin),
                        (Direction4.Left, PlayerAnimation.Walking) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + 0, baseY + Height, Width, Height), new Rectangle(baseX + Width, baseY + Height, Width, Height), new Rectangle(baseX + Width * 2, baseY + Height, Width, Height), new Rectangle(baseX + Width, baseY + Height, Width, Height)), animationSpeed, origin),
                        (Direction4.Right, PlayerAnimation.Walking) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + 0, baseY + Height * 2, Width, Height), new Rectangle(baseX + Width, baseY + Height * 2, Width, Height), new Rectangle(baseX + Width * 2, baseY + Height * 2, Width, Height), new Rectangle(baseX + Width, baseY + Height * 2, Width, Height)), animationSpeed, origin),
                        (Direction4.Up, PlayerAnimation.Walking) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + 0, baseY + Height * 3, Width, Height), new Rectangle(baseX + Width, baseY + Height * 3, Width, Height), new Rectangle(baseX + Width * 2, baseY + Height * 3, Width, Height), new Rectangle(baseX + Width, baseY + Height * 3, Width, Height)), animationSpeed, origin),

                        (Direction4.Down, PlayerAnimation.Idle) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + Width, baseY, Width, Height)), animationSpeed, origin),
                        (Direction4.Left, PlayerAnimation.Idle) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + Width, baseY + Height, Width, Height)), animationSpeed, origin),
                        (Direction4.Right, PlayerAnimation.Idle) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + Width, baseY + Height * 2, Width, Height)), animationSpeed, origin),
                        (Direction4.Up, PlayerAnimation.Idle) => new AnimationSequence<Direction4, PlayerAnimation>(ImmutableArray.Create(new Rectangle(baseX + Width, baseY + Height * 3, Width, Height)), animationSpeed, origin),

                        _ => throw new NotSupportedException()
                    };
                };
            }

            var playerTextrue = this.Content.Load<Texture2D>("Characters");
            this.players = Enumerable.Range(0, 100)
                .Select(i => SpaceSim.Graphics.Sprite.Create(playerTextrue, getPlayerSprite(i)))
                .Select(x => new Animation<Direction4, PlayerAnimation>(x) { SelectedAnimation = PlayerAnimation.Walking })
                .ToArray();
            // var playerSprite 


            base.LoadContent();
        }


        public override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            this.SpriteBatch.Begin(samplerState: SamplerState.PointWrap);

            var selectedIndex = this.selectedChar.x + this.selectedChar.y * NumberOfColumns;

            for (int i = 0; i < this.players.Length; i++)
            {
                var x = i % NumberOfColumns * 43 + 32;
                var y = i / NumberOfColumns * 43 + 43;
                this.players[i].Draw(this.SpriteBatch, new Vector2(x, y), scale: i == selectedIndex ? Vector2.One * 1.2f : Vector2.One);
            }
            this.SpriteBatch.End();
        }

    }
}

