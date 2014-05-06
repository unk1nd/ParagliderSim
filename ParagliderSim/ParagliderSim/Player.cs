using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using OculusRift.Oculus;


namespace ParagliderSim
{
    public class Player : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Game1 game;

        //Player
        Model playerModel;
        const float rotationSpeed = 0.1f;
        const float moveSpeed = 30.0f;
        float lefrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        Matrix playerBodyRotation;
        Matrix playerWorld;
        Vector3 playerPosition = new Vector3(740, 250, -700);
        BoundingSphere playerSphere, originalPlayerSphere;

        //Camera and movement
        MouseState originalMouseState;
        Matrix cameraRotation;
        Vector3 cameraOriginalTarget;
        Vector3 cameraRotatedTarget;
        Vector3 cameraFinalTarget;
        Vector3 rotatedVector;
        Vector3 cameraOriginalUpVector;
        Vector3 cameraRotatedUpVector;

        #region properties
        public Vector3 Position
        {
            get { return playerPosition; }
        }

        #endregion

        public Player(Game1 game)
            : base(game)
        {
            this.game = game;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            playerModel = game.Content.Load<Model>(@"Models/CharacterModelNew");

            initPlayerSphere();
            Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
            originalMouseState = Mouse.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            float timeDifference = (float)gameTime.ElapsedGameTime.TotalSeconds;
            ProcessInput(timeDifference);

            base.Update(gameTime);
        }

        public void Draw()
        {
            Matrix[] transforms = new Matrix[playerModel.Bones.Count];
            playerModel.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                foreach (BasicEffect beffect in mesh.Effects)
                {
                    beffect.EnableDefaultLighting();
                    beffect.World = transforms[mesh.ParentBone.Index] * playerWorld;
                    beffect.View = game.ViewMatrix;
                    beffect.Projection = game.ProjectionMatrix;
                }
                mesh.Draw();
            }
        }

        public void initPlayerSphere()
        {
            foreach (ModelMesh mesh in playerModel.Meshes)
            {
                originalPlayerSphere = BoundingSphere.CreateMerged(originalPlayerSphere, mesh.BoundingSphere);
            }
            originalPlayerSphere = originalPlayerSphere.Transform(Matrix.CreateScale(100.0f));
        }

        #region movement
        private void ProcessInput(float amount)
        {
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
            {
                float deltaX = currentMouseState.X - originalMouseState.X;
                float deltaY = currentMouseState.Y - originalMouseState.Y;
                lefrightRot -= rotationSpeed * deltaX * amount;
                updownRot -= rotationSpeed * deltaY * amount;
                Mouse.SetPosition(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
                //UpdateViewMatrix();
            }

            Vector3 moveVector = new Vector3(0, 0, 0);
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))
                moveVector += new Vector3(0, 0, -1);
            if (keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S))
                moveVector += new Vector3(0, 0, 1);
            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
                moveVector += new Vector3(1, 0, 0);
            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
                moveVector += new Vector3(-1, 0, 0);
            if (keyState.IsKeyDown(Keys.Q))
                moveVector += new Vector3(0, 1, 0);
            if (keyState.IsKeyDown(Keys.Z))
                moveVector += new Vector3(0, -1, 0);
            AddToCameraPosition(moveVector * amount);
            UpdateViewMatrix();
        }

        private void AddToCameraPosition(Vector3 delta)
        {
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);
            rotatedVector = Vector3.Transform(delta, playerBodyRotation);
            playerPosition += rotatedVector * moveSpeed;
        }

        private void UpdateViewMatrix()
        {
            playerBodyRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(lefrightRot);

            if (game.OREnabled)
            {
                cameraRotation = Matrix.CreateFromQuaternion(OculusClient.GetPredictedOrientation()) * playerBodyRotation;
            }
            else
                cameraRotation = playerBodyRotation;

            cameraOriginalTarget = new Vector3(0, 0, -1);
            cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            cameraFinalTarget = playerPosition + cameraRotatedTarget;

            cameraOriginalUpVector = new Vector3(0, 1, 0);
            cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            game.ViewMatrix = Matrix.CreateLookAt(playerPosition, cameraFinalTarget, cameraRotatedUpVector);

            //player
            playerWorld = Matrix.Identity * Matrix.CreateScale(0.01f) * Matrix.CreateRotationY((float)Math.PI) * playerBodyRotation * Matrix.CreateTranslation(playerPosition);
            playerSphere = originalPlayerSphere.Transform(playerWorld);
        }
        #endregion

        #region collision
        public bool checkCollision()
        {
            if (checkTerrainCollision() || checkWorldComponentCollision())
                return true;
            else
                return false;
        }

        public bool checkWorldComponentCollision()
        {
            bool collision = false;

            foreach (WorldComponent wc in game.WorldComponents)
            {
                if (playerSphere.Intersects(wc.getBoundingSphere()))
                    collision = true;
            }
            return collision;
        }

        public bool checkTerrainCollision()
        {
            if (playerPosition.X < 0 || playerPosition.Z > 0 || playerPosition.X > game.Terrain.getWidthUnits() || -playerPosition.Z > game.Terrain.getHeightUnits())
                return false;
            else
            {
                if (playerSphere.Intersects(game.Terrain.getPlane(playerPosition)) == PlaneIntersectionType.Intersecting)
                    return true;
                else
                    return false;
            }
        }
        #endregion
    }
}