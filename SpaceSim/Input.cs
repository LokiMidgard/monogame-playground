using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace SpaceSim
{
    public class Input
    {
        public Vector2 Movment { get; private set; }

        public Direction4 MainDirection { get; private set; }

        public void Updte(GameTime gameTime)
        {

            var keyboard = Keyboard.GetState();

            this.Movment = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.Left))
            {
                this.Movment -= Vector2.UnitX;
                this.MainDirection = Direction4.Left;
            }
            if (keyboard.IsKeyDown(Keys.Right))
            {
                this.Movment += Vector2.UnitX;
                this.MainDirection = Direction4.Right;
            }
            if (keyboard.IsKeyDown(Keys.Up))
            {
                this.Movment -= Vector2.UnitY;
                this.MainDirection = Direction4.Up;
            }
            if (keyboard.IsKeyDown(Keys.Down))
            {
                this.Movment += Vector2.UnitY;
                this.MainDirection = Direction4.Down;
            }
            this.Movment.Normalize();

        }

    }
}

