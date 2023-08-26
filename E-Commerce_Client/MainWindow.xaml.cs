using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace E_Commerce_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Socket ClientSocket=new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
        private const int PORT = 27001;
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void RequestLoop()
        {
            var receiver = Task.Run(() =>
            {
                while (true)
                {
                    ReceiveResponse();
                }
            });
        }

        private void ReceiveResponse()
        {
            var buffer = new byte[10000];
            int received=ClientSocket.Receive(buffer,SocketFlags.None);
            if (received==0) return; 
            var data= new byte[received];
            Array.Copy(buffer, data, received);
            string text=Encoding.ASCII.GetString(data);

            App.Current.Dispatcher.Invoke(() =>
            {
                ResponseTxtb.Text = text;
            });
        }

        private void ConnectToServer()
        {
            while (!ClientSocket.Connected)
            {
                try
                {
                    ClientSocket.Connect(IPAddress.Parse("127.0.0.1"), PORT);
                }
                catch (Exception)
                {

                    throw;
                }
                MessageBox.Show("Connected");
                var buffer = new byte[2048];
                int received = ClientSocket.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data =new byte[received];
                Array.Copy(buffer, data, received);
                string text=Encoding.ASCII.GetString(data);

                App.Current.Dispatcher.Invoke(() =>
                {
                    ParseForView(text);
                });
            }
        }

        private void ParseForView(string text)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var commands = text.Split('\n');
                var result = commands.ToList();
                result.Remove("");
                foreach (var item in result)
                {
                    Button buton = new Button();
                    buton.FontSize=22;
                    buton.Margin=new Thickness(0, 10, 0,0);
                    buton.Content=item;
                    buton.Click+=ButtonClickCommand;
                    CommandsStackpanel.Children.Add(buton);
                }
            });

        }
        TextBox textBox = new TextBox();
        public string SelectedCommand { get; set; }

        private void ButtonClickCommand(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                var content = bt.Content.ToString();
                var result = content.Remove(content.Length-1, 1);
                SelectedCommand=result;

                var splitResult = result.Split('\\');
                if (splitResult.Length>2 || result.Contains('?'))
                {
                    textBox.Width=800;
                    textBox.Height=60;
                    textBox.FontSize=22;
                    if (result.Contains('?'))
                    {
                        var subresult = result.Split(new[] { '?' }, 2);
                        var keyvalues = subresult[1].Split(new[] { '&' }, 2);
                        textBox.Text = "*" + $"{keyvalues[0].Split("=")[0]}&{keyvalues[1].Split("=")[0]}";

                    }
                    else
                        textBox.Text = "*" + splitResult[2];

                    if (ParamsStackPanel.Children.Count>3)
                    {
                        ParamsStackPanel.Children.RemoveAt(3);
                        ParamsStackPanel.Children.RemoveAt(3);
                    }
                    ParamsStackPanel.Children.Add(textBox);
                    Button button = new Button();
                    button.FontSize=22;
                    button.Margin=new Thickness(0, 10, 0, 0);
                    button.Content="Execute";
                    button.Click+=ExecuteClick;
                    ParamsStackPanel.Children.Add(button);
                }
                else
                {
                    if (ParamsStackPanel.Children.Count>3)
                    {
                        ParamsStackPanel.Children.RemoveAt(3);
                        ParamsStackPanel.Children.RemoveAt(3);
                    }
                    SendString(result);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer();
            RequestLoop();
        }

        private void ExecuteClick(object sender, RoutedEventArgs e)
        {
            var result= SelectedCommand.Split("\\");
            var resultText = result[0]+"\\"+result[1]+"\\"+textBox.Text;

            if (SelectedCommand.Contains("json"))
            {
                resultText=result[0]+"\\"+result[1]+" "+textBox.Text;
            }
            else if (SelectedCommand.Contains('?'))
            {
                var subresult = result[1].Split(new[] { '?' }, 2);
                var methodname = subresult[0];
                var keyvalues = subresult[1].Split(new[] { '&' }, 2);
                var values = textBox.Text.Split(new[] { '&' }, 2);
                if (values.Length == 2)
                    resultText= result[0] + "\\" + methodname + "?" + $"{keyvalues[0].Split("=")[0]}=" + values[0] +"&"+ $"{keyvalues[1].Split("=")[0]}=" +values[1];
                else if(values.Length == 1)
                    resultText = result[0] + "\\" + methodname + "?"+ $"{keyvalues[0].Split("=")[0]}=" + values[0];
            }
            SendString(resultText);
        }

        private void SendString(string resultText)
        {
            byte[]buffer=Encoding.ASCII.GetBytes(resultText);
            ClientSocket.Send(buffer,0,buffer.Length,SocketFlags.None);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
