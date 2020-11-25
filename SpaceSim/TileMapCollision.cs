using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Tiled;
using Debug = System.Diagnostics.Debug;

namespace SpaceSim
{
    // Based on Collision classes of MonoGame.Extended
    public class TileMapCollision : IDisposable, IUpdate
    {
        private readonly TiledMap map;
        private readonly List<ICollisionActor<TiledMapTilesetTile>> actors;
        private int length;
        private int start;

        private readonly CollisionTree<TiledMapObject> objectColision;

        public TileMapCollision(TiledMap map, Range? layerRange = null)
        {

            (this.start, this.length) = layerRange?.GetOffsetAndLength(map.Layers.Count) ?? (0, map.Layers.Count);
            this.map = map;
            this.actors = new List<ICollisionActor<TiledMapTilesetTile>>();


            var objectLayersObjects = map.Layers.Skip(this.start).Take(this.length).OfType<TiledMapObjectLayer>().SelectMany(x => x.Objects);
            if (objectLayersObjects.Any())
            {
                this.objectColision = new CollisionTree<TiledMapObject>(new RectangleF(0, 0, map.WidthInPixels, map.HeightInPixels));


                foreach (var obj in objectLayersObjects)
                {
                    if (obj is TiledMapEllipseObject ellipseObject)
                    {
                        var rect = new CircleF(new Point2(
                              x: ellipseObject.Position.X,
                              y: ellipseObject.Position.Y),
                             radius: ellipseObject.Size.Width);
                        this.objectColision.Insert(ellipseObject, rect);

                    }
                    else if (obj is TiledMapPolygonObject polygonObject)
                    {

                    }
                    else if (obj is TiledMapPolylineObject polylineObject)
                    {

                    }
                    else if (obj is TiledMapRectangleObject rectangleObject)
                    {
                        var rect = new RectangleF(
                              x: rectangleObject.Position.X,
                              y: rectangleObject.Position.Y,
                              width: rectangleObject.Size.Width,
                              height: rectangleObject.Size.Height);
                        this.objectColision.Insert(rectangleObject, rect);
                    }


                }
            }
        }



        public void Dispose()
        {
        }

        public void Update(GameTime gameTime)
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var actor in this.actors)
                for (int l = this.start; l < this.length; l++)
                {
                    var layer = this.map.Layers[l];

                    if (layer is TiledMapTileLayer tileLayer)
                    {


                        var tiles = actor is ICollisionActor<TiledMapTilesetTile, RectangleF> rectangleActor
                            ? this.GetCellsOverlappingRectangle(rectangleActor.Bounds, tileLayer)
                            : actor is ICollisionActor<TiledMapTilesetTile, CircleF> circleActor
                            ? this.GetCellsOverlappingRectangle(circleActor.Bounds, tileLayer)
                            : throw new NotSupportedException();


                        foreach (var tile in tiles)
                        {
                            var tileset = this.map.GetTilesetByTileGlobalIdentifier(tile.GlobalIdentifier);

                            if (tileset is null)
                                continue;


                            var firstId = this.map.GetTilesetFirstGlobalIdentifier(tileset);
                            //var tileData = tileset.Tiles[tile.GlobalIdentifier - firstId];

                            TiledMapTilesetTile tileData = null;
                            foreach (var x in tileset.Tiles)
                            {
                                if (x.LocalTileIdentifier == tile.GlobalIdentifier - firstId)
                                {
                                    tileData = x;
                                    break;
                                }
                            }

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

                                bool intersects;
                                Vector2 collisionVector;
                                if (actor is ICollisionActor<TiledMapTilesetTile, RectangleF> rectangleActor2)
                                {
                                    var rect1 = rectangleActor2.Bounds;
                                    intersects = this.Intersects(ref rect1, ref objectRectangle);
                                    collisionVector = intersects
                                        ? PenetrationVector(ref rect1, ref objectRectangle)
                                        : (Vector2)default;
                                }
                                else
                                {
                                    var c = ((ICollisionActor<TiledMapTilesetTile, CircleF>)actor).Bounds;
                                    intersects = this.Intersects(ref c, ref objectRectangle);
                                    collisionVector = intersects
                                        ? PenetrationVector(ref c, ref objectRectangle)
                                        : (Vector2)default;
                                }
                                if (intersects)
                                    actor.OnCollision(new CollisionEventArgs<TiledMapTilesetTile>() { Other = tileData, PenetrationVector = collisionVector });
                            }

                        }
                    }
                    else if (layer is TiledMapObjectLayer objectLayer)
                    {
                        // nothing to do CollisionComponent will handle this
                    }
                    this.objectColision?.Test(actor as ICollisionActor<TiledMapObject>);
                }

        }




        private bool Intersects(ref RectangleF x1, ref RectangleF x2)
        {
            return x1.Intersects(x2);
        }
        private bool Intersects(ref RectangleF x1, ref CircleF x2)
        {
            return Shape.Intersects(x2, x1);
        }
        private bool Intersects(ref CircleF x1, ref RectangleF x2)
        {
            return Shape.Intersects(x1, x2);
        }
        private bool Intersects(ref CircleF x1, ref CircleF x2)
        {
            return x2.Intersects(ref x1);
        }

        private GetOverlappingCellsEnumerable GetCellsOverlappingRectangle(CircleF circle, TiledMapTileLayer tiledMapLayer)
            => this.GetCellsOverlappingRectangle(circle.ToRectangleF(), tiledMapLayer);
        private GetOverlappingCellsEnumerable GetCellsOverlappingRectangle(RectangleF rectangle, TiledMapTileLayer tiledMapLayer)
        {
            return new GetOverlappingCellsEnumerable(ref rectangle, tiledMapLayer, this.map);
        }

        private readonly struct GetOverlappingCellsEnumerable : IEnumerable<TiledMapTile>
        {

            private readonly int sx;
            private readonly int sy;
            private readonly int ex;
            private readonly int ey;
            private readonly TiledMapTileLayer tiledMapLayer;
            private readonly TiledMap map;

            public GetOverlappingCellsEnumerable(ref RectangleF rectangle, TiledMapTileLayer tiledMapLayer, TiledMap map)
            {
                this.sx = MathHelper.Clamp((int)(rectangle.Left / map.TileWidth), 0, map.TileWidth);
                this.sy = MathHelper.Clamp((int)(rectangle.Top / map.TileHeight), 0, map.TileHeight);
                this.ex = MathHelper.Clamp((int)(rectangle.Right / map.TileWidth + 1), 0, map.TileWidth);
                this.ey = MathHelper.Clamp((int)(rectangle.Bottom / map.TileHeight + 1), 0, map.TileHeight);
                this.tiledMapLayer = tiledMapLayer;
                this.map = map;
            }

            public GetOverlappingCellsEnumerator GetEnumerator()
            {
                return new GetOverlappingCellsEnumerator(this);
            }


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            IEnumerator<TiledMapTile> IEnumerable<TiledMapTile>.GetEnumerator()
            {
                return this.GetEnumerator();
            }
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

            public struct GetOverlappingCellsEnumerator : IEnumerator<TiledMapTile>
            {
                private readonly GetOverlappingCellsEnumerable source;
                private int x;
                private int y;

                public GetOverlappingCellsEnumerator(GetOverlappingCellsEnumerable source)
                {
                    this.source = source;
                    this.x = source.sx;
                    this.y = source.sy;
                    this.Current = default;
                }

                public TiledMapTile Current { get; private set; }

                object System.Collections.IEnumerator.Current => throw new NotImplementedException();

                public void Dispose()
                {

                }

                public bool MoveNext()
                {

                    if (this.x < this.source.ex)
                    {
                        this.Current = this.source.tiledMapLayer.Tiles[this.x + this.y * this.source.tiledMapLayer.Width];
                        this.x++;
                        return true;
                    }
                    else if (this.y < this.source.ey)
                    {
                        this.x = this.source.sx;
                        this.Current = this.source.tiledMapLayer.Tiles[this.x + this.y * this.source.tiledMapLayer.Width];
                        this.y++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public void Reset()
                {
                    this.x = this.source.sx;
                    this.y = this.source.sy;
                }
            }
        }


        public void AddActor<TShape>(ICollisionActor<TiledMapTilesetTile, TShape> target)
        where TShape : struct, IShapeF
        {
            this.actors.Add(target);

        }

        //public CollisionGrid CreateGrid(int[] data, int columns, int rows, int cellWidth, int cellHeight)
        //{
        //    if (_grid != null)
        //        throw new InvalidOperationException("Only one collision grid can be created per world");

        //    _grid = new CollisionGrid(data, columns, rows, cellWidth, cellHeight);
        //    return _grid;
        //}

        private static Vector2 PenetrationVector(ref RectangleF rect1, ref RectangleF rect2)
        {
            var intersectingRectangle = RectangleF.Intersection(rect1, rect2);
            Debug.Assert(!intersectingRectangle.IsEmpty,
                "Violation of: !intersect.IsEmpty; Rectangles must intersect to calculate a penetration vector.");

            Vector2 penetration;
            if (intersectingRectangle.Width < intersectingRectangle.Height)
            {
                var d = rect1.Center.X < rect2.Center.X
                    ? intersectingRectangle.Width
                    : -intersectingRectangle.Width;
                penetration = new Vector2(d, 0);
            }
            else
            {
                var d = rect1.Center.Y < rect2.Center.Y
                    ? intersectingRectangle.Height
                    : -intersectingRectangle.Height;
                penetration = new Vector2(0, d);
            }

            return penetration;
        }

        private static Vector2 PenetrationVector(ref CircleF circ1, ref CircleF circ2)
        {
            if (!circ1.Intersects(circ2))
            {
                return Vector2.Zero;
            }

            var displacement = Point2.Displacement(circ1.Center, circ2.Center);

            Vector2 desiredDisplacement;
            if (displacement != Vector2.Zero)
            {
                desiredDisplacement = displacement.NormalizedCopy() * (circ1.Radius + circ2.Radius);
            }
            else
            {
                desiredDisplacement = -Vector2.UnitY * (circ1.Radius + circ2.Radius);
            }


            var penetration = displacement - desiredDisplacement;
            return penetration;
        }

        private static Vector2 PenetrationVector(ref CircleF circ, ref RectangleF rect)
        {
            var collisionPoint = rect.ClosestPointTo(circ.Center);
            var cToCollPoint = collisionPoint - circ.Center;

            if (rect.Contains(circ.Center) || cToCollPoint.Equals(Vector2.Zero))
            {
                var displacement = Point2.Displacement(circ.Center, rect.Center);

                Vector2 desiredDisplacement;
                if (displacement != Vector2.Zero)
                {
                    // Calculate penetration as only in X or Y direction.
                    // Whichever is lower.
                    var dispx = new Vector2(displacement.X, 0);
                    var dispy = new Vector2(0, displacement.Y);
                    dispx.Normalize();
                    dispy.Normalize();

                    dispx *= (circ.Radius + rect.Width / 2);
                    dispy *= (circ.Radius + rect.Height / 2);

                    if (dispx.LengthSquared() < dispy.LengthSquared())
                    {
                        desiredDisplacement = dispx;
                        displacement.Y = 0;
                    }
                    else
                    {
                        desiredDisplacement = dispy;
                        displacement.X = 0;
                    }
                }
                else
                {
                    desiredDisplacement = -Vector2.UnitY * (circ.Radius + rect.Height / 2);
                }

                var penetration = displacement - desiredDisplacement;
                return penetration;
            }
            else
            {
                var normalized = Vector2.Normalize(cToCollPoint);
                var penetration = circ.Radius * normalized - cToCollPoint;
                return penetration;
            }
        }

        private static Vector2 PenetrationVector(ref RectangleF rect, ref CircleF circ)
        {
            return -PenetrationVector(ref circ, ref rect);
        }

    }

    public interface ICollisionActor<TWith, TShape> : ICollisionActor<TWith>
        where TShape : struct, IShapeF
    {
        TShape Bounds { get; }
    }

    public interface ICollisionActor<TWith>
    {
        void OnCollision(in CollisionEventArgs<TWith> collisionInfo);
    }
    public readonly ref struct CollisionEventArgs<TWith>
    {
        public TWith Other { get; init; }
        public Vector2 PenetrationVector { get; init; }
    }




    public class CollisionTree<TWith>
    {
        private readonly Dictionary<ICollisionActor, QuadtreeData> _targetDataDictionary =
            new Dictionary<ICollisionActor, QuadtreeData>();

        private readonly Quadtree _collisionTree;

        private class CollisionWrapper : ICollisionActor
        {
            public CollisionWrapper(TWith obj, IShapeF shape)
            {
                this.Other = obj;
                this.Bounds = shape;
            }

            public TWith Other { get; }
            public IShapeF Bounds { get; }

            public void OnCollision(CollisionEventArgs collisionInfo)
            {
            }
        }


        /// <summary>
        /// Creates a collision tree covering the specified area.
        /// </summary>
        /// <param name="boundary">Boundary of the collision tree.</param>
        public CollisionTree(RectangleF boundary)
        {
            this._collisionTree = new Quadtree(boundary);
        }

        /// <summary>
        /// Update the collision tree and process collisions.
        /// </summary>
        /// <remarks>
        /// Boundary shapes are updated if they were changed since the last
        /// update.
        /// </remarks>
        /// <param name="gameTime"></param>
        public void Test(ICollisionActor<TWith> actor)
        {
            // Detect collisions

            var target = actor;
            var collisions = this._collisionTree.Query(
                target is ICollisionActor<TWith, CircleF> circleTarget
                ? circleTarget.Bounds
                : target is ICollisionActor<TWith, RectangleF> rectTarget
                ? rectTarget.Bounds
                : throw new NotSupportedException()
                );

            // Generate list of collision Infos
            foreach (var other in collisions)
            {
                var collisionInfo = new CollisionEventArgs<TWith>()
                {
                    Other = (other.Target as CollisionWrapper).Other,
                    PenetrationVector = CalculatePenetrationVector(
                        target is ICollisionActor<TWith, CircleF> circleTarget2
                        ? circleTarget2.Bounds
                        : target is ICollisionActor<TWith, RectangleF> rectTarget2
                        ? rectTarget2.Bounds
                        : throw new NotSupportedException(), other.Bounds)
                };

                target.OnCollision(collisionInfo);
            }
        }

        /// <summary>
        /// Inserts the target into the collision tree.
        /// The target will have its OnCollision called when collisions occur.
        /// </summary>
        /// <param name="target">Target to insert.</param>
        public void Insert<TShape>(TWith target, TShape shape)
            where TShape : struct, IShapeF
        {
            var wrapper = new CollisionWrapper(target, shape);
            var data = new QuadtreeData(wrapper);
            this._targetDataDictionary.Add(wrapper, data);
            this._collisionTree.Insert(data);
        }

        #region Penetration Vectors

        /// <summary>
        /// Calculate a's penetration into b
        /// </summary>
        /// <param name="a">The penetrating shape.</param>
        /// <param name="b">The shape being penetrated.</param>
        /// <returns>The distance vector from the edge of b to a's Position</returns>
        private static Vector2 CalculatePenetrationVector(IShapeF a, IShapeF b)
        {
            switch (a)
            {
                case RectangleF rectA when b is RectangleF rectB:
                    return CalculatePenetrationVector(rectA, rectB);
                case CircleF circA when b is CircleF circB:
                    return CalculatePenetrationVector(circA, circB);
                case CircleF circA when b is RectangleF rectB:
                    return CalculatePenetrationVector(circA, rectB);
                case RectangleF rectA when b is CircleF circB:
                    return CalculatePenetrationVector(rectA, circB);
            }

            throw new NotSupportedException("Shapes must be either a CircleF or RectangleF");
        }

        private static Vector2 CalculatePenetrationVector(RectangleF rect1, RectangleF rect2)
        {
            var intersectingRectangle = RectangleF.Intersection(rect1, rect2);
            Debug.Assert(!intersectingRectangle.IsEmpty,
                "Violation of: !intersect.IsEmpty; Rectangles must intersect to calculate a penetration vector.");

            Vector2 penetration;
            if (intersectingRectangle.Width < intersectingRectangle.Height)
            {
                var d = rect1.Center.X < rect2.Center.X
                    ? intersectingRectangle.Width
                    : -intersectingRectangle.Width;
                penetration = new Vector2(d, 0);
            }
            else
            {
                var d = rect1.Center.Y < rect2.Center.Y
                    ? intersectingRectangle.Height
                    : -intersectingRectangle.Height;
                penetration = new Vector2(0, d);
            }

            return penetration;
        }

        private static Vector2 CalculatePenetrationVector(CircleF circ1, CircleF circ2)
        {
            if (!circ1.Intersects(circ2))
            {
                return Vector2.Zero;
            }

            var displacement = Point2.Displacement(circ1.Center, circ2.Center);

            Vector2 desiredDisplacement;
            if (displacement != Vector2.Zero)
            {
                desiredDisplacement = displacement.NormalizedCopy() * (circ1.Radius + circ2.Radius);
            }
            else
            {
                desiredDisplacement = -Vector2.UnitY * (circ1.Radius + circ2.Radius);
            }


            var penetration = displacement - desiredDisplacement;
            return penetration;
        }

        private static Vector2 CalculatePenetrationVector(CircleF circ, RectangleF rect)
        {
            var collisionPoint = rect.ClosestPointTo(circ.Center);
            var cToCollPoint = collisionPoint - circ.Center;

            if (rect.Contains(circ.Center) || cToCollPoint.Equals(Vector2.Zero))
            {
                var displacement = Point2.Displacement(circ.Center, rect.Center);

                Vector2 desiredDisplacement;
                if (displacement != Vector2.Zero)
                {
                    // Calculate penetration as only in X or Y direction.
                    // Whichever is lower.
                    var dispx = new Vector2(displacement.X, 0);
                    var dispy = new Vector2(0, displacement.Y);
                    dispx.Normalize();
                    dispy.Normalize();

                    dispx *= (circ.Radius + rect.Width / 2);
                    dispy *= (circ.Radius + rect.Height / 2);

                    if (dispx.LengthSquared() < dispy.LengthSquared())
                    {
                        desiredDisplacement = dispx;
                        displacement.Y = 0;
                    }
                    else
                    {
                        desiredDisplacement = dispy;
                        displacement.X = 0;
                    }
                }
                else
                {
                    desiredDisplacement = -Vector2.UnitY * (circ.Radius + rect.Height / 2);
                }

                var penetration = displacement - desiredDisplacement;
                return penetration;
            }
            else
            {
                var penetration = circ.Radius * cToCollPoint.NormalizedCopy() - cToCollPoint;
                return penetration;
            }
        }

        private static Vector2 CalculatePenetrationVector(RectangleF rect, CircleF circ)
        {
            return -CalculatePenetrationVector(circ, rect);
        }

        #endregion
    }


    public struct RectangleF2 : IShapeF
    {
        /// <summary>
        /// The x coordinate of the top left corner before rotation.
        /// </summary>
        public float X;
        /// <summary>
        /// The y coordinate of the top left corner before rotation.
        /// </summary>
        public float Y;
        public float Width;
        public float Height;
        /// <summary>
        /// The rotation around the center
        /// </summary>
        public float Rotation;

        public Vector2 Center
        {
            readonly get
            {
                return new Vector2(this.X + this.Width * 0.5f, this.Y + this.Height * 0.5f);
            }
            set
            {
                this.X = value.X - this.Width * 0.5f;
                this.Y = value.Y - this.Height * 0.5f;
            }
        }

        public Vector2 TopLeft
        {
            readonly get
            {
                if (this.Rotation == 0)
                    return new Vector2(this.X, this.Y);

                var vector = new Vector2(this.X, this.Y);
                this.Rotate(ref vector, out vector);
                return vector;
            }
            set
            {
                if (this.Rotation == 0)
                {
                    this.X = value.X;
                    this.Y = value.Y;
                }
                else
                {
                    this.InverseRotate(ref value, out var vector);

                    this.X = vector.X;
                    this.Y = vector.Y;
                }
            }
        }

        public Vector2 TopRight
        {
            readonly get
            {
                if (this.Rotation == 0)
                    return new Vector2(this.X + this.Width, this.Y);

                var vector = new Vector2(this.X + this.Width, this.Y);
                this.Rotate(ref vector, out vector);
                return vector;
            }
            set
            {
                if (this.Rotation == 0)
                {
                    this.X = value.X - this.Width;
                    this.Y = value.Y;
                }
                else
                {
                    this.InverseRotate(ref value, out var vector);

                    this.X = vector.X - this.Width;
                    this.Y = vector.Y;
                }
            }
        }

        public Vector2 BottomLeft
        {
            readonly get
            {
                if (this.Rotation == 0)
                    return new Vector2(this.X, this.Y + this.Height);

                var vector = new Vector2(this.X, this.Y + this.Height);
                this.Rotate(ref vector, out vector);
                return vector;
            }
            set
            {
                if (this.Rotation == 0)
                {
                    this.X = value.X;
                    this.Y = value.Y - this.Height;
                }
                else
                {
                    this.InverseRotate(ref value, out var vector);

                    this.X = vector.X;
                    this.Y = vector.Y - this.Height;
                }
            }
        }

        public Vector2 BottomRight
        {
            readonly get
            {
                if (this.Rotation == 0)
                    return new Vector2(this.X + this.Width, this.Y + this.Height);

                var vector = new Vector2(this.X + this.Width, this.Y + this.Height);
                this.Rotate(ref vector, out vector);
                return vector;
            }
            set
            {
                if (this.Rotation == 0)
                {
                    this.X = value.X - this.Width;
                    this.Y = value.Y - this.Height;
                }
                else
                {
                    this.InverseRotate(ref value, out var vector);

                    this.X = vector.X - this.Width;
                    this.Y = vector.Y - this.Height;
                }
            }

        }

        Point2 IShapeF.Position { readonly get => this.Center; set => this.Center = value; }

        public readonly bool Intersect(ref Vector2 point)
        {

            if (this.Rotation == 0)
            {
                this.InverseRotate(ref point, out var vector);

                return vector.X >= this.X
                    && vector.X <= this.X + this.Width
                    && vector.Y >= this.Y
                    && vector.Y <= this.Y + this.Height;

            }
            return point.X >= this.X
                && point.X <= this.X + this.Width
                && point.Y >= this.Y
                && point.Y <= this.Y + this.Height;
        }

        public readonly EllipseF CreateContaineingElipse()
        {
            return new EllipseF()
            {
                Center = this.Center,
                Rx = this.Width / 2,
                Ry = this.Height / 2,
                Rotation = this.Rotation,
            };
        }


        private readonly void InverseRotate(ref Vector2 point, out Vector2 result)
        {
            var translationMatrix = Matrix.CreateTranslation(-this.X, -this.Y, 0)
                                * Matrix.CreateRotationZ(-this.Rotation)
                                * Matrix.CreateTranslation(this.X, this.Y, 0);
            Vector2.Transform(ref point, ref translationMatrix, out result);
        }
        private readonly void Rotate(ref Vector2 point, out Vector2 result)
        {

            var translationMatrix = Matrix.CreateTranslation(-this.X, -this.Y, 0)
                                * Matrix.CreateRotationZ(this.Rotation)
                                * Matrix.CreateTranslation(this.X, this.Y, 0);
            Vector2.Transform(ref point, ref translationMatrix, out result);
        }
    }
    public struct EllipseF : IShapeF
    {
        public Vector2 Center;
        public float Rx;
        public float Ry;
        /// <summary>
        /// Rotation in radians around center
        /// </summary>
        public float Rotation;

        Point2 IShapeF.Position { get => this.Center; set => this.Center = value; }

        public bool Intersect(ref Vector2 point)
        {
            if (this.Rx == this.Ry)
            {
                Vector2.DistanceSquared(ref this.Center, ref point, out var distance);
                return this.Rx * this.Rx >= distance;
            }
            else if (this.Rotation == 0)
            {
                var distanceX = point.X - this.Center.X;
                var distanceY = point.Y - this.Center.Y;
                var xCalc = (distanceX * distanceX) / (this.Rx * this.Rx);
                var yCalc = (distanceY * distanceY) / (this.Ry * this.Ry);

                return xCalc + yCalc <= 1;
            }
            else
            {
                var translationMatrix = Matrix.CreateTranslation(-this.Center.X, -this.Center.Y, 0)
                    * Matrix.CreateRotationZ(-this.Rotation)
                    * Matrix.CreateTranslation(this.Center.X, this.Center.Y, 0);

                Vector2.Transform(ref point, ref translationMatrix, out var toCheck);

                var distanceX = toCheck.X - this.Center.X;
                var distanceY = toCheck.Y - this.Center.Y;
                var xCalc = (distanceX * distanceX) / (this.Rx * this.Rx);
                var yCalc = (distanceY * distanceY) / (this.Ry * this.Ry);

                return xCalc + yCalc <= 1;

            }
        }

    }

}

