using Microsoft.Xna.Framework;
using System.Collections.Immutable;

namespace SpaceSim.Graphics
{

    public partial record AnimationSequence(ImmutableArray<Rectangle> Frames, float FrameSpeed, Vector2 Origin);
    public partial record AnimationSequence<TDirection, TAnimation>(ImmutableArray<Rectangle> Frames, float FrameSpeed, Vector2 Origin) : AnimationSequence(Frames, FrameSpeed, Origin);

    public partial interface ISprite<TDirection, TAnimation>
        where TDirection : struct, System.Enum
        where TAnimation : struct, System.Enum
    {

    }
}

namespace SpaceSim.Graphics
{
    internal partial class Sprite
    {

    }
}

