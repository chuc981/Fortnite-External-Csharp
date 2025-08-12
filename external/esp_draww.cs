using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace external
{
    // Estrutura PlayerData permanece a mesma
    public struct PlayerData
    {
        public Vector2 HeadScreen;
        public Vector2 BottomScreen;
    }

    public class EspOverlayForm : Form
    {
        // ... (Toda a parte da API do Windows e o construtor permanecem os mesmos) ...
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private List<PlayerData> playersToDraw = new List<PlayerData>();
        private readonly object drawLock = new object();

        public EspOverlayForm()
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.Paint += new PaintEventHandler(Overlay_Paint);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int initialStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, initialStyle | WS_EX_TRANSPARENT);
        }

        public void UpdateEspData(List<PlayerData> newPlayerData)
        {
            lock (drawLock)
            {
                playersToDraw = newPlayerData;
            }
            this.Invalidate();
        }

        // ***** INÍCIO DA CORREÇÃO DEFINITIVA *****
        private void Overlay_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (playersToDraw == null || playersToDraw.Count == 0) return;

            var screenBounds = Screen.PrimaryScreen.Bounds;

            lock (drawLock)
            {
                foreach (var player in playersToDraw)
                {
                    // VERIFICAÇÃO Nº 1 (A MAIS IMPORTANTE): Checa se as coordenadas são válidas.
                    // Se a projeção matemática resultou em infinito ou "Não é um Número", pule este jogador.
                    if (float.IsInfinity((float)player.HeadScreen.X) || float.IsInfinity((float)player.HeadScreen.Y) ||
                        float.IsInfinity((float)player.BottomScreen.X) || float.IsInfinity((float)player.BottomScreen.Y) ||
                        float.IsNaN((float)player.HeadScreen.X) || float.IsNaN((float)player.HeadScreen.Y) ||
                        float.IsNaN((float)player.BottomScreen.X) || float.IsNaN((float)player.BottomScreen.Y))
                    {
                        continue; // Coordenada inválida, ignora completamente.
                    }

                    // Agora que sabemos que os números são válidos, podemos calcular.
                    float boxHeight = (float)(player.BottomScreen.Y - player.HeadScreen.Y);

                    // VERIFICAÇÃO Nº 2: Garante que a caixa tenha uma altura positiva.
                    if (boxHeight <= 0)
                    {
                        continue;
                    }

                    float boxWidth = boxHeight / 2.0f;
                    float boxX = (float)(player.HeadScreen.X - (boxWidth / 2.0f));
                    float boxY = (float)player.HeadScreen.Y;

                    // VERIFICAÇÃO Nº 3 (Segurança extra): Checa se a caixa está muito fora da tela.
                    if (boxX > screenBounds.Width + 200 || boxX + boxWidth < -200 || boxY > screenBounds.Height + 200 || boxY + boxHeight < -200)
                    {
                        continue;
                    }//remove this : )
                    DrawCorneredBox(g, (int)boxX, (int)boxY, (int)boxWidth, (int)boxHeight, Color.Red, 2);
                }
            }
        }
        // ***** FIM DA CORREÇÃO DEFINITIVA *****

        private void DrawCorneredBox(Graphics g, int x, int y, int w, int h, Color color, int thickness)
        {
            using (Pen pen = new Pen(color, thickness))
            {
                float lineLengthW = w / 4.0f;
                float lineLengthH = h / 4.0f;

                g.DrawLine(pen, x, y, x + lineLengthW, y);
                g.DrawLine(pen, x, y, x, y + lineLengthH);
                g.DrawLine(pen, x + w, y, x + w - lineLengthW, y);
                g.DrawLine(pen, x + w, y, x + w, y + lineLengthH);
                g.DrawLine(pen, x, y + h, x + lineLengthW, y + h);
                g.DrawLine(pen, x, y + h, x, y + h - lineLengthH);
                g.DrawLine(pen, x + w, y + h, x + w - lineLengthW, y + h);
                g.DrawLine(pen, x + w, y + h, x + w, y + h - lineLengthH);
            }
        }
    }
}