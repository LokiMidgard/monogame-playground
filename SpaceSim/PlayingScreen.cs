using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Input;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using MonoGame.Extended.ViewportAdapters;
using SpaceSim.Graphics;

namespace SpaceSim
{
    public class PlayingScreen : GameScreen
    {
        #region Initialisation
#pragma warning disable HAA0302 // Display class allocation to capture closure
//#pragma warning disable HAA0301 // Closure Allocation Source

        private Player player;
        private TiledMap _tiledMap;
        private TiledMapRenderer _tiledMapRenderer;
        private OrthographicCamera _camera;
        private Vector2 _cameraPosition;
        private readonly int playerSpriteIndex;
        private TileMapCollision collisonComponent;

        public PlayingScreen(Game1 game, int playerSpriteIndex) : base(game)
        {
            this.playerSpriteIndex = playerSpriteIndex;

        }

        public override void Initialize()
        {
            base.Initialize();
            var viewportadapter = new BoxingViewportAdapter(this.Game.Window, this.GraphicsDevice, 800, 600);
            this._camera = new OrthographicCamera(viewportadapter);
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
            // var playerSprite 

            this.player = new Player(SpaceSim.Graphics.Sprite.Create(playerTextrue, getPlayerSprite(this.playerSpriteIndex)), new Input()) { Position = new Vector2(60, 60) };

            this._tiledMap = this.Content.Load<TiledMap>("Map/test-map");
            this._tiledMapRenderer = new TiledMapRenderer(this.GraphicsDevice, this._tiledMap);

            this.collisonComponent = new TileMapCollision(this._tiledMap);
            this.collisonComponent.AddActor<CircleF>(this.player);

            base.LoadContent();
        }

#pragma warning restore HAA0302 // Display class allocation to capture closure
#pragma warning restore HAA0301 // Closure Allocation Source

        #endregion

        #region Update
        public override void Update(GameTime gameTime)
        {
            var state = KeyboardExtended.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || state.IsKeyDown(Keys.Escape))
                this.Game.GoToCharacterSelection();

            this._tiledMapRenderer.Update(gameTime);
            this._cameraPosition = Vector2.Lerp(this._cameraPosition, this.player.Position, (float)gameTime.ElapsedGameTime.TotalSeconds);
            this._camera.LookAt(this._cameraPosition);

            this._camera.Zoom = 2;

            this.player.Update(gameTime);
            this.collisonComponent.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            this.SpriteBatch.Begin(samplerState: SamplerState.PointWrap, transformMatrix: this._camera.GetViewMatrix());
            this.player.Draw(this.SpriteBatch);
            this._tiledMapRenderer.Draw(this._camera.GetViewMatrix());

            //this.DrawCollisions(gameTime);
            //this.SpriteBatch.DrawCircle(this.player.Bounds, 100, Color.Green);

            this.SpriteBatch.End();
        }

        private void DrawCollisions(GameTime gameTime)
        {

            foreach (var tileLayer in this._tiledMap.TileLayers)
            {
                foreach (var tile in tileLayer.Tiles)
                {
                    var tileset = this._tiledMap.GetTilesetByTileGlobalIdentifier(tile.GlobalIdentifier);

                    if (tileset is null)
                        continue;


                    var firstId = this._tiledMap.GetTilesetFirstGlobalIdentifier(tileset);
                    //var tileData = tileset.Tiles[tile.GlobalIdentifier - firstId];
                    var tileData = tileset.Tiles.FirstOrDefault(x => x.LocalTileIdentifier == tile.GlobalIdentifier - firstId);

                    if (tileData is null)
                        continue;
                    foreach (var tileObject in tileData.Objects)
                    {
                        var objectRectangle = new RectangleF(
                            x: tile.IsFlippedHorizontally
                                ? tileset.TileWidth - (tileObject.Position.X + tileObject.Size.Width)
                                : tileObject.Position.X,
                            y: tile.IsFlippedVertically
                                ? tileset.TileHeight - (tileObject.Position.Y + tileObject.Size.Height)
                                : tileObject.Position.Y,
                            width: tileObject.Size.Width,
                            height: tileObject.Size.Height);


                        // move to correct position in world
                        objectRectangle.Offset(tile.X * tileset.TileWidth, tile.Y * tileset.TileHeight);


                        this.SpriteBatch.FillRectangle(objectRectangle, Color.Red, 0);

                    }
                }

            }

        }

        #endregion

    }
}

