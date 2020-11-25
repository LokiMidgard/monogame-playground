using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Tiled;
using SpaceSim.Graphics;
using System.Linq;

namespace SpaceSim
{
    public class Player : ICollisionActor<TiledMapTilesetTile, CircleF>,
         ICollisionActor<TiledMapObject, CircleF>
    {
        private readonly Input input;

        public Player(ISprite<Direction4, PlayerAnimation> sprite, Input input)
        {
            this.Sprite = sprite;
            this.input = input;
            this.Animation = new Animation<Direction4, PlayerAnimation>(this.Sprite);
        }

        public Vector2 Position { get; set; }
        public Color Color { get; init; }

        public float MovmentSpeed { get; set; } = 64;

        public ISprite<Direction4, PlayerAnimation> Sprite { get; }

        public Animation<Direction4, PlayerAnimation> Animation { get; }

        public CircleF Bounds => new CircleF(new Point2(this.Position.X, this.Position.Y - 8), 12);

        public void Update(GameTime gameTime)
        {
            this.Animation.Update(gameTime);
            this.input.Updte(gameTime);

            this.Position += this.input.Movment * (float)gameTime.ElapsedGameTime.TotalSeconds * this.MovmentSpeed;
            this.Animation.Direction = this.input.MainDirection;
            if (this.input.Movment == Vector2.Zero)
                this.Animation.SelectedAnimation = PlayerAnimation.Idle;
            else
                this.Animation.SelectedAnimation = PlayerAnimation.Walking;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Animation.Draw(spriteBatch, this.Position);
        }


        public void OnCollision(in CollisionEventArgs<TiledMapTilesetTile> collisionInfo)
        {
            this.Position -= collisionInfo.PenetrationVector;
        }

        public void OnCollision(in CollisionEventArgs<TiledMapObject> collisionInfo)
        {
            this.Position -= collisionInfo.PenetrationVector;
        }
    }
}

