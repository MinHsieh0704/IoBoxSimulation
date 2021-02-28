using Min_Helpers;
using Min_Helpers.PrintHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IoBoxSimulation
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// DI 群組
        /// </summary>
        private Grid[] diGroups = null;

        /// <summary>
        /// DI off radio buttons
        /// </summary>
        private RadioButton[] rbDIOffs = null;

        /// <summary>
        /// DI on radio buttons
        /// </summary>
        private RadioButton[] rbDIOns = null;

        /// <summary>
        /// current simulation status
        /// </summary>
        private bool isOpen = false;

        private Socket server { get; set; }

        private List<Socket> clients { get; set; }

        private CancellationTokenSource tokenSource { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            InitializeState();
        }

        private void btn_action_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isOpen)
                {
                    CloseSimulation();
                }
                else
                {
                    OpenSimulation();
                }
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                App.PrintService.Log(ex.Message, Print.EMode.error);

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void input_port_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // validate number only
                Regex regex = new Regex("^[0-9]+$");
                e.Handled = !regex.IsMatch(e.Text);

                if (e.Handled == true) return;

                // validate number range 1 - 65535
                int min = 1;
                int max = 65535;

                int input = int.Parse(input_port.Text + e.Text);

                e.Handled = !(input >= min && input <= max);
            }
            catch (Exception ex)
            {
                e.Handled = true;
            }

        }

        private void input_di_count_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                // validate number range 2 - 4
                Regex regex = new Regex("^[2-4]+$");
                e.Handled = !regex.IsMatch(e.Text);
            }
            catch (Exception ex)
            {
                e.Handled = true;
            }
        }

        private void input_di_count_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int diCount = int.Parse(input_di_count.Text);
                InitializeDIControls(diCount);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Initialize State
        /// </summary>
        private void InitializeState()
        {
            diGroups = new Grid[]
            {
                g_di0,
                g_di1,
                g_di2,
                g_di3
            };

            rbDIOffs = new RadioButton[]
            {
                rb_di0_off,
                rb_di1_off,
                rb_di2_off,
                rb_di3_off
            };

            rbDIOns = new RadioButton[]
            {
                rb_di0_on,
                rb_di1_on,
                rb_di2_on,
                rb_di3_on
            };

            input_port.Text = "12345";
            input_di_count.Text = "2";

            rb_di0_off.IsChecked = true;
            rb_di1_off.IsChecked = true;
            rb_di2_off.IsChecked = true;
            rb_di3_off.IsChecked = true;

            CloseSimulation();
        }

        /// <summary>
        /// Open Simulation
        /// </summary>
        private void OpenSimulation()
        {
            isOpen = true;
            btn_action.Content = "close";

            input_port.IsEnabled = false;
            input_di_count.IsEnabled = false;

            EnableAllDIRadioButtons(true);

            IPAddress ipAddress = IPAddress.Any;
            int port = Convert.ToInt32(this.input_port.Text);

            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, port);

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(iPEndPoint);
            server.Listen(100);

            tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(() =>
            {
                clients = new List<Socket>();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Socket client = server.Accept();
                        EndPoint remote = client.RemoteEndPoint;

                        App.PrintService.Log($"Client<{remote}> is connecting", Print.EMode.success);

                        clients.Add(client);

                        Task.Run(() =>
                        {
                            try
                            {
                                while (!token.IsCancellationRequested && !(client.Poll(1, SelectMode.SelectRead) && client.Available == 0))
                                {
                                    byte[] bytes = new byte[1024];

                                    int bytesRec = client.Receive(bytes);

                                    string message = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                    message = message.Replace("\n", "").Replace("\r", "");

                                    if (message == "AT+STACH0=?")
                                    {
                                        this.Dispatcher.Invoke(new Action(() =>
                                        {
                                            string disStatus = rbDIOns
                                                .Take(Convert.ToInt32(input_di_count.Text))
                                                .Select((n, i) => $"+STACH{i + 1}:{(n.IsChecked == true ? 1 : 0)},100000")
                                                .Aggregate((prev, curr) => prev += $"\r\n{curr}");

                                            byte[] byteData = Encoding.ASCII.GetBytes($"{disStatus}\r\n");
                                            client.Send(byteData);
                                        }));
                                    }
                                }

                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                            }
                            catch (Exception ex)
                            {
                            }
                            finally
                            {
                                if (clients != null)
                                {
                                    clients = clients.Where((n) => n.Handle != client.Handle).ToList();
                                    App.PrintService.Log($"Client<{remote}> was disconnected", Print.EMode.warning);
                                }
                            }
                        }, token);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }, token);
        }

        /// <summary>
        /// Close Simulation
        /// </summary>
        private void CloseSimulation()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();

                tokenSource = null;
            }
            if (clients != null)
            {
                for (int i = 0; i < clients.Count(); i++)
                {
                    var client = clients[i];
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    client.Dispose();
                }

                clients = null;
            }
            if (server != null)
            {
                server.Close();
                server.Dispose();
                server = null;
            }

            isOpen = false;
            btn_action.Content = "open";

            input_port.IsEnabled = true;
            input_di_count.IsEnabled = true;

            EnableAllDIRadioButtons(false);
        }

        /// <summary>
        /// Enable or Disable all DI radio buttons
        /// </summary>
        /// <param name="isEnabled"></param>
        private void EnableAllDIRadioButtons(bool isEnabled)
        {
            for (int i = 0; i < diGroups.Length; i++)
            {
                rbDIOffs[i].IsEnabled = isEnabled;
                rbDIOns[i].IsEnabled = isEnabled;
            }
        }

        /// <summary>
        /// Initialize DI controls
        /// </summary>
        /// <param name="diCount">2 - 4</param>
        private void InitializeDIControls(int diCount)
        {
            input_di_count.Text = diCount.ToString();

            for (int i = 0; i < diGroups.Length; i++)
            {
                diGroups[i].Visibility = Visibility.Hidden;
            }

            for (int i = 0; i < diCount; i++)
            {
                diGroups[i].Visibility = Visibility.Visible;
            }
        }
    }
}
