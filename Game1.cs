﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Poly2Tri;
using Poly2Tri.Triangulation.Polygon;
using Test_Layer_Points;
using System.Windows.Forms;
using System.Linq;

namespace WinformMonoGame
{
    public enum DrawShapeType
    {
        RECTANGLE,
        CIRCLE,
        POLYGON,
        LIMIT
    }

    public class ShapeInfo
    {
        public List<Vector2> points;
        public Color color;
        public float thickness;

        public ShapeInfo()
        {
            points = new List<Vector2>();
            color = new Color(255, 255, 255, 255);
            thickness = 1.0f;
        }

        public ShapeInfo(Color _color, int _thickness)
        {
            points = new List<Vector2>();
            color = _color;
            thickness = _thickness;
        }
    }

    public class LayerInfo
    {
        public List<VertexPositionColor> triangleVertices;
        public bool isDraw;
        public Color foreColor;
        public Texture2D texture;
        public int transparency;
        public List<ShapeInfo> shapes;

        public LayerInfo()
        {
            triangleVertices = new List<VertexPositionColor>();
            isDraw = false;
            shapes = new List<ShapeInfo>();
        }

        public void DrawShape(SpriteBatch spriteBatch)
        {
            texture.SetData(new[] { Color.White });
            for (int i = 0; i < shapes.Count; i++)
            {
                if(shapes[i].points.Count > 1)
                {
                    for(int j = 1; j < shapes[i].points.Count; j++)
                    {
                        Vector2 start = shapes[i].points[j - 1];
                        Vector2 end = shapes[i].points[j];
                        Vector2 edge = end - start;
                        float angle = (float)Math.Atan2(edge.Y, edge.X);
                        float length = edge.Length();

                        spriteBatch.Draw(texture,
                            new Rectangle((int)start.X, (int)start.Y, (int)length, (int)shapes[i].thickness),
                            null,
                            shapes[i].color,
                            angle,
                            Vector2.Zero,
                            SpriteEffects.None,
                            0);
                    }
                }
            }
        }
    }

    public class Game1 : Game
    {
        public GraphicsDeviceManager graphics;
        private IntPtr drawSurface;
        private BasicEffect basicEffect;
        private SpriteBatch spriteBatch;
        public RenderTarget2D renderTarget;

        public List<LayerInfo> layers;

        private List<VertexPositionColor> drawingVertices;

        private Vector2 posOffset;
        private double scale = 0.1;
        public bool isDragging = false;
        public Vector2 prevmousePosition = new Vector2(0, 0);
        public int mouse_delta = 0;
        public Vector2 mouse_position = new Vector2(0, 0);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public Game1(IntPtr drawSurface) : this()
        {
            this.drawSurface = drawSurface;
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphics_PreparingDeviceSettings);
            System.Windows.Forms.Control.FromHandle((this.Window.Handle)).VisibleChanged += new EventHandler(Game1_VisibleChanged);
        }

        private void OnClientSizeChanged(object sender, System.EventArgs e)
        {
            // 윈도우 크기 변경 시 백버퍼 크기 조정
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();

            drawingVertices = new List<VertexPositionColor>();
            posOffset = Vector2.Zero;
            layers = new List<LayerInfo>();
            // for draw primitives
            layers.Add(new LayerInfo());
            layers[0].texture = new Texture2D(GraphicsDevice, 1, 1);
        }

        protected override void LoadContent()
        {
            basicEffect = new BasicEffect(GraphicsDevice);
            basicEffect.VertexColorEnabled = true;

            spriteBatch = new SpriteBatch(GraphicsDevice);
            renderTarget = new RenderTarget2D(GraphicsDevice, Window.ClientBounds.Width, Window.ClientBounds.Height);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Exit();

            Vector2 mousePosition = mouse_position;

            if (mouse_delta != 0)
            {
                double scaleChange = mouse_delta > 0 ? 1.1 : 0.9;
                Vector2 centerToMouse = mousePosition - posOffset;
                scale *= scaleChange;
                posOffset = mousePosition - centerToMouse * (float)scaleChange;
                mouse_delta = 0;
            }

            if (isDragging)
            {
                posOffset += (mousePosition - prevmousePosition);
                prevmousePosition = mousePosition;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter
            (
                0, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, 0,
                0, 1
            );

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //stopwatch.Stop();
            //System.Windows.Forms.MessageBox.Show($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");

            GraphicsDevice.Clear(Color.Black);

            for (int i = 1; i < layers.Count; i++)
            {
                if (layers[i].isDraw)
                {
                    drawingVertices.Clear();
                    for (int j = 0; j < layers[i].triangleVertices.Count; j++)
                    {
                        double x = (double)layers[i].triangleVertices[j].Position.X * scale + (double)posOffset.X;
                        double y = (double)layers[i].triangleVertices[j].Position.Y * scale + (double)posOffset.Y;
                        drawingVertices.Add(new VertexPositionColor(new Vector3((float)x, (float)y, 0), layers[i].triangleVertices[j].Color));
                        //drawingVertices.Add(new VertexPositionColor(new Vector3((float)layers[i].triangleVertices[j].Position.X, (float)layers[i].triangleVertices[j].Position.Y, 0), layers[i].triangleVertices[j].Color));
                    }

                    GraphicsDevice.SetRenderTarget(renderTarget);

                    spriteBatch.Begin();
                    foreach (var pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawUserPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, drawingVertices.ToArray(), 0, drawingVertices.Count / 3);
                    }
                    spriteBatch.End();

                    layers[i].texture = renderTarget;
                    layers[i].texture = ApplyTransparency(layers[i].texture, GraphicsDevice, layers[i].foreColor, layers[i].transparency);
                    GraphicsDevice.SetRenderTarget(null);
                }
            }

            spriteBatch.Begin();
            if (layers[0].shapes.Count > 0)
            {
                layers[0].DrawShape(spriteBatch);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].isDraw)
                {
                    //spriteBatch.Draw(layers[i].texture, Vector2.Zero, Color.White);
                    //spriteBatch.Draw(layers[i].texture, new Vector2(i * 100, i * 100), null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, (float)(i / layers.Count));
                    //spriteBatch.Draw(layers[i].texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2((float)scale, (float)scale), SpriteEffects.None, (float)(i / layers.Count));
                    spriteBatch.Draw(layers[i].texture, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, (float)(i / layers.Count));
                }
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        Texture2D ApplyTransparency(Texture2D originalTexture, GraphicsDevice graphicsDevice, Color fore, int transparency)
        {
            Texture2D newTexture = new Texture2D(graphicsDevice, originalTexture.Width, originalTexture.Height);
            Color[] pixelData = new Color[originalTexture.Width * originalTexture.Height];

            originalTexture.GetData(pixelData);

            for (int i = 0; i < pixelData.Length; i++)
            {
                if(pixelData[i].R == fore.R && pixelData[i].G == fore.G && pixelData[i].B == fore.B)
                {
                    pixelData[i] = new Color(fore, transparency);
                }
                else
                {
                    pixelData[i] = new Color(0, 0, 0, 0);
                }
            }

            newTexture.SetData(pixelData);

            return newTexture;
        }

        private List<PolygonPoint> RealToScreen(List<PolygonPoint> points)
        {
            List<PolygonPoint> ret = new List<PolygonPoint>();

            for (int i = 0; i < points.Count; i++)
            {
                double x = (double)points[i].X * scale + (double)posOffset.X;
                double y = (double)points[i].Y * scale + (double)posOffset.Y;
                ret.Add(new PolygonPoint(x, y));
            }

            return ret;
        }

        /// <summary>
        /// Event capturing the construction of a draw surface and makes sure this gets redirected to
        /// a predesignated drawsurface marked by pointer drawSurface
        /// </summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.DeviceWindowHandle = drawSurface;
        }

        /// <summary>
        /// Occurs when the original gamewindows' visibility changes and makes sure it stays invisible
        /// </summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void Game1_VisibleChanged(object sender, EventArgs e)
        {
            if (System.Windows.Forms.Control.FromHandle((this.Window.Handle)).Visible == true)
                System.Windows.Forms.Control.FromHandle((this.Window.Handle)).Visible = false;
        }
    }
}
