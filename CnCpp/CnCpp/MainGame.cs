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
using CCClasses.FileFormats.Text;
using CCClasses.FileFormats.Binary;

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

        private HVA MotLib;
        private VXL Voxel;
        private bool VoxelChanged;
        private int VoxelFrame;

        private VXL.VertexPositionColorNormal[] VoxelContent;
        private int[] VoxelIndices;

        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;

        BasicEffect effect;
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;


        private int defOX = 0, defOY = 0;
        private Vector3 defRotation = new Vector3(MathHelper.PiOver4, -1 * MathHelper.PiOver4, -3 * MathHelper.PiOver4);

        private int offX, offY;
        private Vector3 rotation;

        private float scale = 1f;



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

            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;

            offX = defOX;
            offY = defOY;
            rotation = defRotation;
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

                            case ".MIX":
                                var M = new MIX(file);

                                Console.WriteLine("Loaded MIX with {0} entries", M.Entries.Count);
                                break;


                            case ".HVA":
                                var H = new HVA(file);

                                Console.WriteLine("Loaded HVA with {0} sections", H.Sections.Count);
                                break;


                            case ".VXL":
                                var hvaName = file.Replace(Path.GetExtension(file), ".hva");
                                if (File.Exists(hvaName)) {
                                    MotLib = new HVA(hvaName);

                                    Voxel = new VXL(file);
                                    VoxelChanged = true;
                                    VoxelFrame = 0;

                                    Console.WriteLine("Loaded VXL with {0} sections", Voxel.Sections.Count);

                                    foreach (var S in MotLib.Sections) {
                                        Console.WriteLine("Section####");
                                        foreach (var TM in S.T) {
                                            Console.WriteLine(@"Section:
{0:0.00}; {1:0.00}; {2:0.00};     {3:0.00}
{4:0.00}; {5:0.00}; {6:0.00};     {7:0.00}
{8:0.00}; {9:0.00}; {10:0.00};     {11:0.00}"
                                                , TM.M11, TM.M12, TM.M13, TM.M14
                                                , TM.M21, TM.M22, TM.M23, TM.M24
                                                , TM.M31, TM.M32, TM.M33, TM.M34
                                            );
                                        }
                                    }

                                    Voxel.SetHVA(MotLib);

                                } else {
                                    System.Windows.Forms.MessageBox.Show(String.Format("Failed to load HVA file corresponding to VXL {0} ({1})", file, hvaName));
                                }
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

            if (kState.IsKeyDown(Keys.Space)) {
                offX = defOX;
                offY = defOY;
                rotation = defRotation;
                scale = 1f;
            }

            if (kState.IsKeyDown(Keys.Up)) {
                offY -= (int)scale;
            } else if (kState.IsKeyDown(Keys.Down)) {
                offY += (int)scale;
            }

            if (kState.IsKeyDown(Keys.Left)) {
                offX -= (int)scale;
            } else if (kState.IsKeyDown(Keys.Right)) {
                offX += (int)scale;
            }

            var rot = MathHelper.PiOver4 / 4;

            if (kState.IsKeyDown(Keys.Q)) {
                rotation.X += rot;
            } else if (kState.IsKeyDown(Keys.A)) {
                rotation.X -= rot;
            }

            if (kState.IsKeyDown(Keys.W)) {
                rotation.Y += rot;
            } else if (kState.IsKeyDown(Keys.S)) {
                rotation.Y -= rot;
            }

            if (kState.IsKeyDown(Keys.E)) {
                rotation.Z += rot;
            } else if (kState.IsKeyDown(Keys.D)) {
                rotation.Z -= rot;
            }

            if (kState.IsKeyDown(Keys.Multiply)) {
                scale *= 1.01f;
            } else if (kState.IsKeyDown(Keys.Divide)) {
                scale /= 1.01f;
            }

            if (kState.IsKeyDown(Keys.L)) {
                effect.LightingEnabled = !effect.LightingEnabled;
            }

            if (Voxel != null) {
                var fcount = (int)(MotLib.Header.FrameCount - 1);
                if (kState.IsKeyDown(Keys.Z)) {
                    if (VoxelFrame < fcount) {
                        ++VoxelFrame;
                    } else {
                        VoxelFrame = 0;
                    }
                    VoxelChanged = true;
                } else if (kState.IsKeyDown(Keys.X)) {
                    if (VoxelFrame > 0) {
                        --VoxelFrame;
                    } else {
                        VoxelFrame = fcount;
                    }
                    VoxelChanged = true;
                }
            }


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

            if (VoxelChanged) {
                VoxelChanged = false;
                if (MousePalette != null) {
                    Voxel.GetVertices(MousePalette, VoxelFrame, out VoxelContent, out VoxelIndices);

                    Console.WriteLine("Loaded {0} vertices and {1} indices", VoxelContent.Length, VoxelIndices.Length);

                    if (vertexBuffer != null) {
                        vertexBuffer.Dispose();
                    }

                    // Initialize the vertex buffer, allocating memory for each vertex.
                    vertexBuffer = new VertexBuffer(graphics.GraphicsDevice, VXL.VertexPositionColorNormal.VertexDeclaration, VoxelContent.Length, BufferUsage.WriteOnly);

                    // Set the vertex buffer data to the array of vertices.
                    vertexBuffer.SetData<VXL.VertexPositionColorNormal>(VoxelContent);

                    indexBuffer = new IndexBuffer(graphics.GraphicsDevice, typeof(int), VoxelIndices.Length, BufferUsage.WriteOnly);

                    indexBuffer.SetData(VoxelIndices);

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
            GraphicsDevice.RasterizerState = new RasterizerState() {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None
            };

            //if (CurrentMouseTexture != null) {

            //    //var bs = new BlendState();
            //    //bs.AlphaSourceBlend = Blend.One;
            //    //bs.AlphaDestinationBlend = Blend.InverseSourceAlpha;
            //    //bs.ColorSourceBlend = Blend.One;
            //    //bs.ColorDestinationBlend = Blend.Zero;
            //    //bs.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue | ColorWriteChannels.Alpha;

            //    spriteBatch.Begin();

            //    spriteBatch.Draw(CurrentMouseTexture, MousePos, Color.White);

            //    spriteBatch.End();
            //}


            if (VoxelContent != null) {
                worldMatrix = Matrix.Identity * Matrix.CreateScale(scale);

                effect.World = worldMatrix;

                projectionMatrix = Matrix.CreateOrthographic((float)GraphicsDevice.Viewport.Width, (float)GraphicsDevice.Viewport.Height, -1000.0f, 1000.0f);

                effect.Projection = projectionMatrix;

                viewMatrix = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y) * Matrix.CreateRotationZ(rotation.Z) * Matrix.CreateTranslation(offX, offY, 0);

                effect.View = viewMatrix;
                

                GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GraphicsDevice.Indices = indexBuffer;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();

                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VoxelContent.Length, 0, VoxelIndices.Length / 3);
                }
            }

            base.Draw(gameTime);
        }
    }
}
