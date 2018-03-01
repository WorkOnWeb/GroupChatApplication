using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormClient.SVC;

namespace WinFormClient
{
    public partial class Form1 : Form, SVC.IChatCallback
    {
        SVC.ChatClient _Proxy = null;
        SVC.Client _LocalClient = null;


        Dictionary<string, SVC.Client> OnlineClients = new Dictionary<string, Client>();
        private delegate void FaultedInvoker();

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        #region Events
        private void Form1_Load(object sender, EventArgs e)
        {
            ShowChat(false);
            ShowLogin(true);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            lblMessage.Text = "Connecting..";
            _Proxy = null;
            Connect();
        }
        private void btnLogOut_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Proxy == null || _Proxy.State != CommunicationState.Opened || _Proxy.State != CommunicationState.Opening)
            {
                AutoValidate = AutoValidate.Disable;
                return;
            }
            Disconnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Send();
        }
        #endregion

        #region Logic
        //Loose
        private void HandleProxy()
        {
            if (_Proxy != null)
            {
                switch (this._Proxy.State)
                {
                    case CommunicationState.Closed:
                        _Proxy = null;
                        lstChat.Items.Clear();
                        lstMembers.Items.Clear();
                        lblMessage.Text = "Disconnected";
                        ShowChat(false);
                        ShowLogin(true);
                        btnLogin.Enabled = true;
                        break;
                    case CommunicationState.Closing:
                        break;
                    case CommunicationState.Created:
                        break;
                    case CommunicationState.Faulted:
                        _Proxy.Abort();
                        _Proxy = null;
                        lstChat.Items.Clear();
                        lstChat.Items.Clear();
                        ShowChat(false);
                        ShowLogin(true);
                        lblMessage.Text = "Disconnected";
                        btnLogin.Enabled = true;
                        break;
                    case CommunicationState.Opened:
                        ShowLogin(false);
                        ShowChat(true);

                        lblStatusMessage.Text = "Connected";

                        break;
                    case CommunicationState.Opening:
                        break;
                    default:
                        break;
                }
            }
            else
            {
                _Proxy = null;
                lstChat.Items.Clear();
                lstMembers.Items.Clear();
                lblMessage.Text = "Disconnected";
                ShowChat(false);
                ShowLogin(true);
                btnLogin.Enabled = true;
            }
        }
        private void Connect()
        {
            if (_Proxy == null)
            {
                try
                {
                    this._LocalClient = new SVC.Client();
                    this._LocalClient.Name = txtUserName.Text.ToString();
                    InstanceContext _Context = new InstanceContext(this);
                    _Proxy = new SVC.ChatClient(_Context);

                    //As the address in the configuration file is set to localhost
                    //we want to change it so we can call a service in internal 
                    //network, or over internet
                    string servicePath = _Proxy.Endpoint.ListenUri.AbsolutePath;
                    string serviceListenPort = txtPort.Text.ToString();

                    _Proxy.Endpoint.Address = new EndpointAddress("net.tcp://" + txtServerName.Text.ToString() + ":" + serviceListenPort + servicePath);
                    _Proxy.Open();

                    _Proxy.InnerDuplexChannel.Faulted += new EventHandler(InnerDuplexChannel_Faulted);
                    _Proxy.InnerDuplexChannel.Opened += new EventHandler(InnerDuplexChannel_Opened);
                    _Proxy.InnerDuplexChannel.Closed += new EventHandler(InnerDuplexChannel_Closed);
                    _Proxy.ConnectAsync(this._LocalClient);
                    _Proxy.ConnectCompleted += new EventHandler<ConnectCompletedEventArgs>(_Proxy_ConnectCompleted);
                }
                catch (Exception ex)
                {
                    //loginTxtBoxUName.Text = ex.Message.ToString();
                    lblMessage.Text = "Something Went Wrong. Can't connect to server";
                    btnLogin.Enabled = true;
                }
            }
            else
            {
                //Loose
                HandleProxy();
            }
        }
        private void Disconnect()
        {
            try
            {
                if (_Proxy != null)
                {
                    if (_Proxy.State == CommunicationState.Faulted)
                    {
                    }
                    else
                    {
                        _Proxy.Disconnect(this._LocalClient);
                    }
                }
            }
            catch (Exception Ex)
            {
                _Proxy = null;
            }
            finally
            {
                HandleProxy();
            }
        }
        private void Send()
        {
            if (_Proxy != null)
            {
                if (_Proxy.State == CommunicationState.Faulted)
                {
                    HandleProxy();
                }
                else
                {
                    SVC.Message msg = new SVC.Message();
                    msg.Sender = this._LocalClient.Name;
                    msg.Content = txtMessageBox.Text.ToString();

                    _Proxy.SayAsync(msg);

                    string _Hi = _Proxy.InnerChannel.State.ToString();
                    string _Hi2 = _Proxy.InnerDuplexChannel.State.ToString();

                    txtMessageBox.Text = "";
                    txtMessageBox.Focus();
                }

            }
        }

        void InnerDuplexChannel_Closed(object sender, EventArgs e)
        {
            this.BeginInvoke(new FaultedInvoker(HandleProxy));
        }
        void InnerDuplexChannel_Opened(object sender, EventArgs e)
        {
            this.BeginInvoke(new FaultedInvoker(HandleProxy));
        }
        void InnerDuplexChannel_Faulted(object sender, EventArgs e)
        {
            this.BeginInvoke(new FaultedInvoker(HandleProxy));
        }
        void _Proxy_ConnectCompleted(object sender, ConnectCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                lblMessage.Text = e.Error.Message.ToString();
                btnLogin.Enabled = true;
            }
            else if (e.Result)
            {
                HandleProxy();
            }
            else if (!e.Result)
            {
                lblMessage.Text = "Username taken. Try another username.";
                btnLogin.Enabled = true;
            }
        }

        #endregion

        #region VisibilityLogin
        private void ShowChat(bool _P_Visibility)
        {
            pnlChat.Visible = _P_Visibility;
        }
        private void ShowLogin(bool _P_Visibility)
        {
            pnlLogin.Visible = _P_Visibility;
        }
        #endregion

        public void RefreshClients(List<Client> clients)
        {
            lstMembers.Items.Clear();
            OnlineClients.Clear();
            foreach (SVC.Client c in clients)
            {
                lstMembers.Items.Add(c.Name);
                OnlineClients.Add(c.Name, c);
            }
        }
        public void Receive(SVC.Message msg)
        {
            foreach (SVC.Client c in this.OnlineClients.Values)
            {
                if (c.Name == msg.Sender)
                {
                    lstChat.Items.Add(c.Name + " => " + msg.Content);
                }
            }
        }
        public void UserJoin(Client client)
        {
            string _T_Messgae = @"------------ " + client.Name + " joined chat ------------";
            lstChat.Items.Add(_T_Messgae);
        }
        public void UserLeave(Client client)
        {
            string _T_Messgae = @"------------ " + client.Name + " left chat ------------";
            lstChat.Items.Add(_T_Messgae);
        }

        #region Loop
        private void btnLoop_Click(object sender, EventArgs e)
        {
            SingleLoopSend();
        }
        private void btnCancelLoop_Click(object sender, EventArgs e)
        {
            if (_BackgroundWorkerForLoop.IsBusy)
            {
                _BackgroundWorkerForLoop.CancelAsync();
            }
        }
        
        BackgroundWorker _BackgroundWorkerForLoop;
        private void SingleLoopSend()
        {
            try
            {
                _BackgroundWorkerForLoop = new BackgroundWorker();
                _BackgroundWorkerForLoop.DoWork += _BackgroundWorkerForLoop_DoWork;
                _BackgroundWorkerForLoop.ProgressChanged += _BackgroundWorkerForLoop_ProgressChanged;
                _BackgroundWorkerForLoop.RunWorkerCompleted += _BackgroundWorkerForLoop_RunWorkerCompleted;
                _BackgroundWorkerForLoop.WorkerSupportsCancellation = true;
                btnLoop.Enabled = false;

                _BackgroundWorkerForLoop.RunWorkerAsync();
            }
            catch
            {
                btnLoop.Enabled = true;
            }
        }
        private void _BackgroundWorkerForLoop_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_Proxy != null)
            {
                if (_Proxy.State == CommunicationState.Faulted)
                {
                    HandleProxy();
                }
                else
                {
                    for (int i = 0; i <= 50; i++)
                    {
                        //Create message, assign its properties

                        if ((_BackgroundWorkerForLoop.CancellationPending == true))
                        {
                            e.Cancel = true;
                            return;
                        }

                        SVC.Message msg = new SVC.Message();
                        msg.Sender = this._LocalClient.Name;
                        msg.Content = "From Single Loop" + i.ToString();

                        _Proxy.SayAsync(msg);
                        System.Threading.Thread.Sleep(2000);
                    }
                }
            }
        }
        private void _BackgroundWorkerForLoop_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnLoop.Enabled = true;
        }
        private void _BackgroundWorkerForLoop_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        #endregion

        private void txtServerName_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtServerName.Text))
            {
                e.Cancel = true;
                txtServerName.Focus();
                ClientErrorProvider.SetError(txtServerName, "Server IP should not be left blank!");
            }
            else
            {
                e.Cancel = false;
                ClientErrorProvider.SetError(txtServerName, "");
            }
        }
        private void txtPort_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPort.Text))
            {
                e.Cancel = true;
                txtPort.Focus();
                ClientErrorProvider.SetError(txtPort, "Server Port should not be left blank!");
            }
            else if (!Regex.IsMatch(txtPort.Text, @"\d"))
            {
                e.Cancel = true;
                txtPort.Focus();
                ClientErrorProvider.SetError(txtPort, "Port must be number");
            }
            else
            {
                e.Cancel = false;
                ClientErrorProvider.SetError(txtPort, "");
            }
        }
        private void txtUserName_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                e.Cancel = true;
                txtUserName.Focus();
                ClientErrorProvider.SetError(txtUserName, "User name should not be left blank!");
            }
            else
            {
                e.Cancel = false;
                ClientErrorProvider.SetError(txtUserName, "");
            }
        }


        #region Async
        public IAsyncResult BeginUserLeave(SVC.Client client, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }
        public void EndUserLeave(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginUserJoin(SVC.Client client, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }
        public void EndUserJoin(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginReceive(SVC.Message msg, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }
        public void EndReceive(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginRefreshClients(List<Client> clients, AsyncCallback callback, object asyncState)
        {
            throw new NotImplementedException();
        }
        public void EndRefreshClients(IAsyncResult result)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
