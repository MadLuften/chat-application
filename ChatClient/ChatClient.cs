using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ChatClient
{
    [ServiceContract(CallbackContract = typeof(IChatSystem))]
    public interface IChatSystem
    {
        [OperationContract(IsOneWay = true)]
        void Join(string userName);
        [OperationContract(IsOneWay = true)]
        void Leave(string userName);
        [OperationContract(IsOneWay = true)]
        void Message(string userName, string userMsg);
    }

    public interface IChatChannel : IChatSystem, IClientChannel
    {
    }

    public partial class ChatClient : Form, IChatSystem
    {
        private delegate void JoinNewUser(string name);
        private delegate void SendNewMsg(string name, string newMsg);
        private delegate void LeaveChat(string name);

        private static event JoinNewUser JoinUser;
        private static event SendNewMsg SendMsg;
        private static event LeaveChat LeaveUser;
                
        private string clientName;
        private IChatChannel chtChnl;
        private DuplexChannelFactory<IChatChannel> factory;

        public ChatClient()
        {
            InitializeComponent();
            this.AcceptButton = btnJoin;
        }

        public ChatClient(string clientName)
        {
            this.clientName = clientName;
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtName.Text.Trim()))
            {
                try
                {
                    JoinUser += new JoinNewUser(ChatClient_JoinUser);
                    SendMsg += new SendNewMsg(ChatClient_SendMsg);
                    LeaveUser += new LeaveChat(ChatClient_LeaveUser);

                    chtChnl = null;
                    this.clientName = txtName.Text.Trim();
                    InstanceContext context = new InstanceContext(
                        new ChatClient(txtName.Text.Trim()));
                    factory =
                        new DuplexChannelFactory<IChatChannel>(context, "ChatEndPoint");
                    chtChnl = factory.CreateChannel();
                    IOnlineStatus status = chtChnl.GetProperty<IOnlineStatus>();
                    status.Offline += new EventHandler(Offline);
                    status.Online += new EventHandler(Online);                    
                    chtChnl.Open();                    
                    chtChnl.Join(this.clientName);             
                    this.AcceptButton = btnSend;
                    txtChat.AppendText("Welcome to this chat room");
                    txtSend.Select();
                    txtSend.Focus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        void ChatClient_JoinUser(string name)
        {
            txtChat.AppendText("\r\n");
            txtChat.AppendText(name + " joined at: [" + DateTime.Now.ToString() + "]");
            lstUsers.Items.Add(name);
        }
        
        void ChatClient_SendMsg(string name, string msg)
        {
            if (!lstUsers.Items.Contains(name))
            {
                lstUsers.Items.Add(name);
            }
            txtChat.AppendText("\r\n");
            txtChat.AppendText(name + " says: " + msg);
        }

        void ChatClient_LeaveUser(string name)
        {
            try
            {
                txtChat.AppendText("\r\n");
                txtChat.AppendText(name + " left at " + DateTime.Now.ToString());
                lstUsers.Items.Remove(name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }

        void Online(object sender, EventArgs e)
        {            
            txtChat.AppendText("\r\nOnline: " + this.clientName);
        }

        void Offline(object sender, EventArgs e)
        {
            txtChat.AppendText("\r\nOffline: " + this.clientName);
        }

        #region IChatSystem Members

        public void Join(string clientName)
        {            
            if (JoinUser != null)
            {
                JoinUser(clientName);
            }
        }        

        public void Message(string clientName, string msg)
        {
            if (SendMsg != null) SendMsg(clientName, msg);
        }

        public new void Leave(string clientName)
        {
            if (LeaveUser != null) LeaveUser(clientName);
        }

        #endregion

        private void btnSend_Click(object sender, EventArgs e)
        {
            chtChnl.Message(this.clientName, txtSend.Text.Trim());
            txtSend.Clear();
            txtSend.Select();
            txtSend.Focus();
        }

        private void ChatClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (chtChnl != null)
                {
                    chtChnl.Leave(this.clientName);
                    chtChnl.Close();
                }
                if (factory != null)
                {
                    factory.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void txtSend_TextChanged(object sender, EventArgs e)
        {

        }

        
    }
}