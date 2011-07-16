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
using System.IO;
using CCClasses;
using CCClasses.FileFormats;

namespace CnCpp {
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class MainGame : Microsoft.Xna.Framework.Game {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private readonly object plock = new object();
        private System.Windows.Forms.Form form;

        private SHP MouseTextures;
        private PAL MousePalette;

        private INI INIFile;

        private Texture2D CurrentMouseTexture;
        private int MouseFrame;
        private int MouseScroll;
        private Vector2 MousePos;

        public MainGame() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // TODO: Add your initialization logic here

            base.Initialize();

            form = System.Windows.Forms.Form.FromHandle(Window.Handle) as System.Windows.Forms.Form;

            if (form == null) {
                throw new InvalidOperationException("Unable to get underlying Form.");
            }

            InitializeDragDrop();

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
        }

        private void InitializeDragDrop() {
            form.AllowDrop = true;
            form.DragEnter += new System.Windows.Forms.DragEventHandler(form_DragEnter);
            form.DragOver += new System.Windows.Forms.DragEventHandler(form_DragOver);
            form.DragDrop += new System.Windows.Forms.DragEventHandler(form_DragDrop);
        }

        void form_DragEnter(object sender, System.Windows.Forms.DragEventArgs e) {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
        }

        void form_DragOver(object sender, System.Windows.Forms.DragEventArgs e) {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
        }

        void form_DragDrop(object sender, System.Windows.Forms.DragEventArgs e) {

            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

                if (files != null) {
                    lock (plock) {
                        var file = files[0];
                        var ext = Path.GetExtension(file).ToUpper();

                        switch (ext) {
                            case ".PAL":
                                MousePalette = new PAL(file);

                                break;

                            case ".SHP":
                            case ".SHA":
                                MouseFrame = -1;

                                MouseTextures = new SHP(file);
                                if (MousePalette == null) {
                                    MousePalette = PAL.GrayscalePalette;
                                }
                                MouseTextures.ApplyPalette(MousePalette);
                                break;

                            case ".INI":
                                INIFile = new INI(file);

                                break;
                        }

                    }
                }
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            var kState = Keyboard.GetState();
            if (kState.IsKeyDown(Keys.Escape))
                this.Exit();

            // TODO: Add your update logic here

            var pos = Mouse.GetState();

            MousePos.X = pos.X;
            MousePos.Y = pos.Y;

            bool MouseFrameChanged = false;

            lock (plock) {
                if (MouseTextures == null) {
                    CurrentMouseTexture = null;
                } else {
                    if (MouseFrame == -1) {
                        ++MouseFrame;
                        MouseFrameChanged = true;
                    }

                    if (kState.IsKeyDown(Keys.Up) || (pos.ScrollWheelValue > MouseScroll)) {
                        if ((MouseFrame + 1) < (int)MouseTextures.FrameCount) {
                            ++MouseFrame;
                            MouseFrameChanged = true;
                        }
                    } else if (kState.IsKeyDown(Keys.Down) || (pos.ScrollWheelValue < MouseScroll)) {
                        if (MouseFrame > -1) {
                            --MouseFrame;
                            MouseFrameChanged = true;
                        }
                    }

                    MouseScroll = pos.ScrollWheelValue;

                    if (MouseFrameChanged) {
                        if (MouseTextures != null && MouseFrame > -1) {
                            CurrentMouseTexture = MouseTextures.GetTexture((uint)MouseFrame, graphics.GraphicsDevice);
                        }
                    }

                    if (CurrentMouseTexture != null && kState.IsKeyDown(Keys.S)) {
                        var outdir = Directory.GetCurrentDirectory();
                        var outfile = Path.Combine(outdir, "scr.png");
                        using (FileStream s = File.OpenWrite(outfile)) {
                            CurrentMouseTexture.SaveAsPng(s, CurrentMouseTexture.Width, CurrentMouseTexture.Height);
                        }
                    }
                }
            }
            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            if (CurrentMouseTexture != null) {

                //var bs = new BlendState();
                //bs.AlphaSourceBlend = Blend.One;
                //bs.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                //bs.ColorSourceBlend = Blend.One;
                //bs.ColorDestinationBlend = Blend.Zero;
                //bs.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue | ColorWriteChannels.Alpha;

                spriteBatch.Begin();

                spriteBatch.Draw(CurrentMouseTexture, MousePos, Color.White);

                spriteBatch.End();
            }
            
            base.Draw(gameTime);
        }
    }
}
