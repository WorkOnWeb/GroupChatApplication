using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ServiceHost _ServiceHost;
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren(ValidationConstraints.Enabled))
            {
                return;
            }
            string _Address = "";
            try
            {
                btnStart.Enabled = false;

                //Define base addresses so all endPoints can go under it

                Uri tcpAdrs = new Uri("net.tcp://" + txtServerIP.Text.ToString() + ":" +
                    int.Parse(txtServerPort.Text.ToString()).ToString() + "/ChatServer/");

                Uri httpAdrs = new Uri("http://" + txtServerIP.Text.ToString() + ":" +
                    (int.Parse(txtServerPort.Text.ToString()) + 1).ToString() + "/ChatServer/");

                Uri[] baseAdresses = { tcpAdrs, httpAdrs };
                _ServiceHost = new ServiceHost(typeof(ServiceAssembly.ChatService), baseAdresses);


                NetTcpBinding _NetTcpBinding = new NetTcpBinding(SecurityMode.None, true);
                //Updated: to enable file transefer of 64 MB
                _NetTcpBinding.MaxBufferPoolSize = (int)67108864;
                _NetTcpBinding.MaxBufferSize = 67108864;
                _NetTcpBinding.MaxReceivedMessageSize = (int)67108864;
                _NetTcpBinding.TransferMode = TransferMode.Buffered;
                _NetTcpBinding.ReaderQuotas.MaxArrayLength = 67108864;
                _NetTcpBinding.ReaderQuotas.MaxBytesPerRead = 67108864;
                _NetTcpBinding.ReaderQuotas.MaxStringContentLength = 67108864;


                _NetTcpBinding.MaxConnections = 10000;
                //To maxmize MaxConnections you have to assign another port for mex endpoint

                //and configure ServiceThrottling as well
                ServiceThrottlingBehavior throttle;
                throttle = _ServiceHost.Description.Behaviors.Find<ServiceThrottlingBehavior>();
                if (throttle == null)
                {
                    throttle = new ServiceThrottlingBehavior();
                    throttle.MaxConcurrentCalls = 10000;
                    throttle.MaxConcurrentSessions = 10000;
                    _ServiceHost.Description.Behaviors.Add(throttle);
                }


                //Enable reliable session and keep the connection alive for 20 hours.
                _NetTcpBinding.ReceiveTimeout = new TimeSpan(20, 0, 0);
                _NetTcpBinding.ReliableSession.Enabled = true;
                _NetTcpBinding.ReliableSession.InactivityTimeout = new TimeSpan(20, 0, 10);

                _ServiceHost.AddServiceEndpoint(typeof(ServiceAssembly.IChat), _NetTcpBinding, "tcp");

                //Define Metadata endPoint, So we can publish information about the service
                ServiceMetadataBehavior mBehave = new ServiceMetadataBehavior();
                _ServiceHost.Description.Behaviors.Add(mBehave);


                _Address = "net.tcp://" + txtServerIP.Text.ToString() + ":" +
                    (int.Parse(txtServerPort.Text.ToString()) - 1).ToString() + "/ChatServer/mex";

                _ServiceHost.AddServiceEndpoint(typeof(IMetadataExchange),
                    MetadataExchangeBindings.CreateMexTcpBinding(), _Address);

                _ServiceHost.Open();
            }
            catch (System.UriFormatException UriEx)
            {
                lblStatus.Text = "Server / Port name is incorrect or Server  is not reachable. Please check details again.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message.ToString();
            }
            finally
            {
                if (_ServiceHost != null && _ServiceHost.State == CommunicationState.Opened)
                {
                    lblStatus.Text = "Server Running :" + _Address;
                    btnStopServer.Enabled = true;
                    txtServerIP.Enabled = false;
                    txtServerPort.Enabled = false;
                    btnStart.Enabled = false;
                }
                else
                {
                    btnStart.Enabled = true;
                }
            }
        }
        private void btnStopServer_Click(object sender, EventArgs e)
        {
            StopServer();
        }
        private void txtServerIP_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                e.Cancel = true;
                txtServerIP.Focus();
                ServerHostErrorProvider.SetError(txtServerIP, "Server IP should not be left blank!");
            }
            else
            {
                e.Cancel = false;
                ServerHostErrorProvider.SetError(txtServerIP, "");
            }
        }
        private void txtServerPort_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtServerPort.Text))
            {
                e.Cancel = true;
                txtServerPort.Focus();
                ServerHostErrorProvider.SetError(txtServerPort, "Server Port should not be left blank!");
            }
            else if (!Regex.IsMatch(txtServerPort.Text, @"\d"))
            {
                e.Cancel = true;
                txtServerPort.Focus();
                ServerHostErrorProvider.SetError(txtServerPort, "Port must be number");
            }
            else
            {
                e.Cancel = false;
                ServerHostErrorProvider.SetError(txtServerPort, "");
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            AutoValidate = AutoValidate.Disable;
            StopServer();
            this.Close();
        }
        private void StopServer()
        {
            if (_ServiceHost != null)
            {
                try
                {
                    _ServiceHost.Close();
                }
                catch (Exception ex)
                {
                    lblStatus.Text = ex.Message.ToString();
                }
                finally
                {
                    if (_ServiceHost.State == CommunicationState.Closed)
                    {
                        lblStatus.Text = "Server Stoped";
                        btnStopServer.Enabled = false;
                        txtServerIP.Enabled = true;
                        txtServerPort.Enabled = true;
                        btnStart.Enabled = true;
                    }
                }
            }
        }

    }
}
