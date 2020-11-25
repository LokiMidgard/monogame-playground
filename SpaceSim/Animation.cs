using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceSim.Graphics;
namespace SpaceSim
{
    //public class Sprite<TDirection, TAnimations>
    //where TDirection : struct, System.Enum
    //where TAnimations : struct, System.Enum
    //{
    //    public Texture2D Texture { get; }
    //    public float FrameSpeed { get; set; } = 1.0f;
    //    public Animation this[TDirection direction, TAnimations animation]
    //        => this.animations[Convert.ToInt32(direction), Convert.ToInt32(animation)];

    //    private readonly Animation[,] animations;
    //    public Sprite(Texture2D sheet, Func<TDirection, TAnimations, Animation> animationSelection)
    //    {
    //        this.Texture = sheet;
    //        TDirection[] directions = Enum.GetValues<TDirection>();
    //        TAnimations[] animations1 = Enum.GetValues<TAnimations>();
    //        this.animations = new Animation[directions.Length, animations1.Length];
    //        SpaceSim.Graphics.Sprites.Create(null, (Direction4 direction, DefaultAnimation animation) =>
    //        {
    //            return new Animation(ImmutableArray<Rectangle>.Empty, 1f);
    //        });
    //        SpaceSim.Graphics.Sprites.Create(null, (Direction4 direction, DefaultAnimation animation) =>
    //        {
    //            return new Animation(ImmutableArray<Rectangle>.Empty, 1f);
    //        });
    //        foreach (var direction in directions)
    //            foreach (var animation in animations1)
    //                this.animations[Convert.ToInt32(direction), Convert.ToInt32(animation)] = animationSelection(direction, animation);
    //    }


    //}







    public class Animation<TDirection, TAnimation>
        where TDirection : struct, Enum
        where TAnimation : struct, Enum
    {
        private TimeSpan animationStart;
        private TimeSpan lastGameTime;

        public ISprite<TDirection, TAnimation> Sprite { get; }

        private TDirection direction;
        private TAnimation animation;

        public Animation(ISprite<TDirection, TAnimation> sprite)
        {
            this.Sprite = sprite;
        }

        public TDirection Direction
        {
            get => this.direction;
            set
            {
                if (!EqualityComparer<TDirection>.Default.Equals(this.direction, value))
                {
                    this.direction = value;
                    this.animationStart = this.lastGameTime;
                }
            }
        }
        public TAnimation SelectedAnimation
        {
            get => this.animation;
            set
            {
                if (!EqualityComparer<TAnimation>.Default.Equals(this.animation, value))
                {
                    this.animation = value;
                    this.animationStart = this.lastGameTime;
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            this.lastGameTime = gameTime.TotalGameTime;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Vector2? scale = null, Color? color = null, float rotation = 0, float layer = 0)
        {
            var diff = this.lastGameTime - this.animationStart;
            var ani = this.Sprite[this.Direction, this.SelectedAnimation];
            var index = (int)((diff.TotalSeconds * ani.FrameSpeed) % ani.Frames.Length);
            spriteBatch.Draw(this.Sprite.Texture, position, ani.Frames[index], color ?? Color.White, rotation, ani.Origin, scale ?? Vector2.One, SpriteEffects.None, layer);
        }

    }
}

