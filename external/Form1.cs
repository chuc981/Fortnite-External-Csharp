using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace external
{
    public partial class Form1 : Form
    {
        private TextBox logTextBox;
        private EspOverlayForm overlayForm; // Variável para controlar nossa janela de desenho

        private bool isEspEnabled = false;
        private Task espTask;
        private CancellationTokenSource cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
            logTextBox = new TextBox();
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.ReadOnly = true;
            logTextBox.Dock = DockStyle.Fill;
            groupBox1.Controls.Add(logTextBox);
        }

        private void Log(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => logTextBox.AppendText(message + Environment.NewLine)));
            }
            else
            {
                logTextBox.AppendText(message + Environment.NewLine);
            }
        }

        private void Log(ulong address, string prefix = "")
        {
            logTextBox.AppendText($"{prefix}0x{address:X}" + Environment.NewLine);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Driver.Init())
            {
                Log("Falha ao abrir handle do driver.");
                return;
            }
            else
            {
                Log("OK abrir handle do driver.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Log("MOUSE MOVED");
            Driver.MoveMouse(300, 300, 0);
        }

        nint base_addres = 0;
        private void button3_Click(object sender, EventArgs e)
        {
            int process_id = Driver.GetProcessId("FortniteClient-Win64-Shipping.exe");
            Log($"process_id: {process_id}");
            base_addres = Driver.GetBaseAddress();
            Log($"base_addres: {base_addres}");
            bool cr3_start = Driver.CR3();
            Log($"cr3_start: {cr3_start}");

            nint driver_read = Driver.Read<nint>((ulong)base_addres);
            Log($"driver_read: {driver_read}");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isEspEnabled = !isEspEnabled;

            if (isEspEnabled)
            {
                Log("ESP Ativado.");
                button4.Text = "Parar ESP";

                // Cria e mostra a janela de overlay em sua própria thread
                Thread overlayThread = new Thread(() =>
                {
                    overlayForm = new EspOverlayForm();
                    Application.Run(overlayForm); // Mantém a janela viva
                });
                overlayThread.SetApartmentState(ApartmentState.STA);
                overlayThread.IsBackground = true;
                overlayThread.Start();

                // Inicia a thread que lê os dados do jogo
                cancellationTokenSource = new CancellationTokenSource();
                espTask = Task.Run(() => EspLoop(cancellationTokenSource.Token));
            }
            else
            {
                Log("ESP Desativado.");
                button4.Text = "Iniciar ESP";

                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }

                // Fecha a janela de overlay de forma segura
                overlayForm?.Invoke(new Action(() => overlayForm.Close()));
                overlayForm = null;
            }
        }

        private void EspLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Garante que o loop só rode se o overlay existir e o ponteiro for válido
                if (overlayForm == null || overlayForm.IsDisposed) break;
                if (!Driver.IsValidPointer(base_addres))
                {
                    Thread.Sleep(100);
                    continue;
                }

                var currentFramePlayers = new List<PlayerData>();

                nint Uworld = Driver.Read<nint>((ulong)base_addres + 0x18047488);
                nint game_state = Driver.Read<nint>((ulong)Uworld + 0x1C0);
                nint game_instace = Driver.Read<nint>((ulong)Uworld + 0x238);
                nint local_player_index = Driver.Read<nint>((ulong)game_instace + 0x38);
                nint local_player = Driver.Read<nint>((ulong)local_player_index);
                nint player_controller = Driver.Read<nint>((ulong)local_player + 0x30);
                nint player_array = Driver.Read<nint>((ulong)game_state + 0x2C0);
                int player_count = Driver.Read<int>((ulong)game_state + 0x2C0 + (ulong)IntPtr.Size);

                for (int i = 0; i < player_count; ++i)
                {
                    ulong player_state = Driver.Read<ulong>((ulong)player_array + (ulong)(i * IntPtr.Size));
                    if (player_state == 0) continue;

                    nint pawn_private = Driver.Read<nint>((ulong)player_state + 0x320);
                    if (pawn_private == 0) continue;

                    nint mesh = Driver.Read<nint>((ulong)pawn_private + 0x328);
                    if (mesh == 0) continue;

                    Vector3 bones = GameUtils.GetEntityBone((ulong)mesh, 110);
                    Vector3 bottom3d = GameUtils.GetEntityBone((ulong)mesh, 0);
                    CameraViewPoint camera = Projection.GetViewPoint((ulong)Uworld, (ulong)player_controller);
                    Vector2 head2d = Projection.ProjectWorldToScreen(bones, camera);
                    Vector2 bottom2d = Projection.ProjectWorldToScreen(bottom3d, camera);

                    if (head2d.X > 0 || head2d.Y > 0 || bottom2d.X > 0 || bottom2d.Y > 0)
                    {
                        currentFramePlayers.Add(new PlayerData { HeadScreen = head2d, BottomScreen = bottom2d });
                    }
                }

                // Envia os dados para a janela de overlay se ela ainda existir
                try
                {
                    overlayForm?.Invoke(new Action(() => overlayForm.UpdateEspData(currentFramePlayers)));
                }
                catch (ObjectDisposedException)
                {
                    // A janela foi fechada, então o loop pode parar.
                    break;
                }

                Thread.Sleep(5);
            }
        }
    }
}