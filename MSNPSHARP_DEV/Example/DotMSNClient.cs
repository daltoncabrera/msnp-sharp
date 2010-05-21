using System;
using System.IO;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// MSNPSharp Client example.
    /// </summary>
    public partial class ClientForm : System.Windows.Forms.Form
    {
        // Create a Messenger object to use MSNPSharp.
        private Messenger messenger = new Messenger();
        private List<ConversationForm> convforms = new List<ConversationForm>(0);
        private TraceForm traceform = new TraceForm();
        private Dictionary<string, ConversationForm> YIMMessageForms = new Dictionary<string, ConversationForm>(0);

        public List<ConversationForm> ConversationForms
        {
            get
            {
                return convforms;
            }
        }

        public Messenger Messenger
        {
            get
            {
                return messenger;
            }
        }

        public ClientForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.Icon = Properties.Resources.MSNPSharp_logo_small_ico;

            // You can set proxy settings here
            // for example: messenger.ConnectivitySettings.ProxyHost = "10.0.0.2";


            Settings.TraceSwitch.Level = System.Diagnostics.TraceLevel.Verbose;

            if (Settings.IsMono) //I am running on Mono.
            {
                // Don't enable this on mono, because mono raises NotImplementedException.
                Settings.EnableGzipCompressionForWebServices = false;
            }

#if DEBUG

            //How to save your personal addressbook.
            //If you want your addressbook have a better reading/writting performance, use MclSerialization.None
            //In this case, your addressbook will be save as a xml file, everyone can read it.
            //If you want your addressbook has a smaller size, use MclSerialization.Compression.
            //In this case, your addressbook file will be save in gzip format, none can read it, but the performance is not so good.
            Settings.SerializationType = MSNPSharp.IO.MclSerialization.None;
#elif TRACE
            Settings.SerializationType = MSNPSharp.IO.MclSerialization.Compression | MSNPSharp.IO.MclSerialization.Cryptography;
#endif

            // set the events that we will handle
            // remember that the nameserver is the server that sends contact lists, notifies you of contact status changes, etc.
            // a switchboard server handles the individual conversation sessions.
            messenger.NameserverProcessor.ConnectionEstablished += new EventHandler<EventArgs>(NameserverProcessor_ConnectionEstablished);
            messenger.Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
            messenger.Nameserver.SignedOff += new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff);
            messenger.NameserverProcessor.ConnectingException += new EventHandler<ExceptionEventArgs>(NameserverProcessor_ConnectingException);
            messenger.Nameserver.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred);
            messenger.Nameserver.AuthenticationError += new EventHandler<ExceptionEventArgs>(Nameserver_AuthenticationError);
            messenger.Nameserver.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived);
            messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(messenger_ConversationCreated);
            messenger.Nameserver.CrossNetworkMessageReceived += new EventHandler<CrossNetworkMessageEventArgs>(Nameserver_CrossNetworkMessageReceived);
            messenger.TransferInvitationReceived += new EventHandler<MSNSLPInvitationEventArgs>(messenger_TransferInvitationReceived);
            messenger.Nameserver.PingAnswer += new EventHandler<PingAnswerEventArgs>(Nameserver_PingAnswer);

            messenger.Nameserver.ContactOnline += new EventHandler<ContactEventArgs>(Nameserver_ContactOnline);
            messenger.Nameserver.ContactOffline += new EventHandler<ContactEventArgs>(Nameserver_ContactOffline);


            messenger.ContactService.ReverseAdded += new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded);
            messenger.ContactService.SynchronizationCompleted += new EventHandler<EventArgs>(ContactService_SynchronizationCompleted);


            #region Circle events

            messenger.ContactService.CreateCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_CircleCreated);
            messenger.ContactService.JoinedCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_JoinedCircle);
            messenger.ContactService.JoinCircleInvitationReceived += new EventHandler<JoinCircleInvitationEventArgs>(ContactService_JoinCircleInvitationReceived);
            messenger.ContactService.ExitCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_ExitCircle);
            messenger.ContactService.CircleMemberJoined += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberJoined);
            messenger.ContactService.CircleMemberLeft += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberLeft);
            messenger.Nameserver.CircleOnline += new EventHandler<CircleEventArgs>(Nameserver_CircleOnline);
            messenger.Nameserver.CircleOffline += new EventHandler<CircleEventArgs>(Nameserver_CircleOffline);
            messenger.Nameserver.CircleMemberOnline += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberOnline);
            messenger.Nameserver.CircleMemberOffline += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberOffline);
            messenger.Nameserver.LeftCircleConversation += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberLeftConversation);
            messenger.Nameserver.JoinedCircleConversation += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleMemberJoinedConversation);
            messenger.Nameserver.CircleTextMessageReceived += new EventHandler<CircleTextMessageEventArgs>(Nameserver_CircleTextMessageReceived);
            messenger.Nameserver.CircleNudgeReceived += new EventHandler<CircleMemberEventArgs>(Nameserver_CircleNudgeReceived); 

            #endregion


            #region Offline Message Operation events

            messenger.OIMService.OIMReceived += new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived);
            messenger.OIMService.OIMSendCompleted += new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted); 

            #endregion
            

            messenger.WhatsUpService.GetWhatsUpCompleted += WhatsUpService_GetWhatsUpCompleted;

            #region Webservice Error handler

            // Handle Service Operation Errors
            //In most cases, these error are not so important.
            messenger.ContactService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.OIMService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.StorageService.ServiceOperationFailed += ServiceOperationFailed;
            messenger.WhatsUpService.ServiceOperationFailed += ServiceOperationFailed; 

            #endregion
        }

        void Nameserver_CrossNetworkMessageReceived(object sender, CrossNetworkMessageEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<CrossNetworkMessageEventArgs>(Nameserver_CrossNetworkMessageReceived), new object[] { sender, e });
            }
            else
            {
                ConversationForm convForm = null;
                if (YIMMessageForms.ContainsKey(e.From.Mail.ToLowerInvariant()))
                {
                    convForm = YIMMessageForms[e.From.Mail.ToLowerInvariant()];
                }
                else
                {
                    convForm = new ConversationForm(Messenger, e.From);
                    YIMMessageForms[e.From.Mail.ToLowerInvariant()] = convForm;
                }

                convForm.OnCrossNetworkMessageReceived(sender, e);
            }
        }

        public static class ImageIndexes
        {
            public const int Closed = 0;
            public const int Open = 1;
            public const int Circle = 2;

            public const int Online = 3;
            public const int Busy = 4;
            public const int Away = 5;
            public const int Idle = 6;
            public const int Hidden = 7;
            public const int Offline = 8;

            // Show always (0/0)
            public const string FavoritesNodeKey = "__10F";
            public const string CircleNodeKey = "__20C";
            // Sort by status (0)
            public const string MobileNodeKey = "__30M";
            public const string OnlineNodeKey = "__32N";
            public const string OfflineNodeKey = "__34F";
            // Sort by categories (0/0)
            public const string NoGroupNodeKey = "ZZZZZ";
            
            public static int GetStatusIndex(PresenceStatus status)
            {
                switch (status)
                {
                    case PresenceStatus.Online:
                        return Online;

                    case PresenceStatus.Busy:
                    case PresenceStatus.Phone:
                        return Busy;

                    case PresenceStatus.BRB:
                    case PresenceStatus.Away:
                    case PresenceStatus.Lunch:
                        return Away;

                    case PresenceStatus.Idle:
                        return Idle;
                    case PresenceStatus.Hidden:
                        return Hidden;

                    case PresenceStatus.Offline:
                        return Offline;

                    default:
                        return Offline;
                }
            }
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.closed);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.open);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.circle);

            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.online);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.busy);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.away);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.idle);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.hidden);
            ImageList1.Images.Add(MSNPSharpClient.Properties.Resources.offline);

            // Move PM panel to SignIn window...
            pnlNameAndPM.Location = panel1.Location;
            Version dllVersion = messenger.GetType().Assembly.GetName().Version;
            Text += "(v" + dllVersion.Major + "." + dllVersion.Minor + "." + dllVersion.Build + " r" + dllVersion.Revision + ")";
            treeViewFavoriteList.TreeViewNodeSorter = StatusSorter.Default;

            comboStatus.SelectedIndex = 0;
            comboProtocol.SelectedIndex = 0;

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            // ******* Listen traces *****
            traceform.Show();
        }


        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Messenger.Connected)
            {
                Messenger.Nameserver.SignedOff -= Nameserver_SignedOff;
                
                ResetAll();
                Messenger.Disconnect();
            }

            traceform.Close();
        }

        void Nameserver_CircleMemberOffline(object sender, CircleMemberEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void Nameserver_CircleMemberOnline(object sender, CircleMemberEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void Nameserver_CircleNudgeReceived(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle " + e.Circle.ToString() + ": Member: " + e.Member.ToString() + " send you a nudge.");
            AutoGroupMessageReply(e.Circle);
        }

        private void AutoGroupMessageReply(Circle circle)
        {
            if (Messenger.ContactList.Owner.Status != PresenceStatus.Hidden || Messenger.ContactList.Owner.Status != PresenceStatus.Offline)
                circle.SendMessage(new TextMessage("MSNPSharp example client auto reply."));
        }

        void Nameserver_CircleTextMessageReceived(object sender, CircleTextMessageEventArgs e)
        {
            Trace.WriteLine("Circle " + e.Sender.ToString() + ": Member: " + e.TriggerMember.ToString() + " send you a message :" + e.Message.ToString());
            AutoGroupMessageReply(e.Sender);
        }

        void Nameserver_CircleMemberJoinedConversation(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle member " + e.Member.ToString() + " joined the circle conversation: " + e.Circle.ToString());
        }

        void Nameserver_CircleMemberLeftConversation(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Circle member " + e.Member.ToString() + " has left the circle: " + e.Circle.ToString());
        }

        void Nameserver_CircleOnline(object sender, CircleEventArgs e)
        {
            Trace.WriteLine("Circle go online: " + e.Circle.ToString());
            RefreshCircleList(sender, e);
        }

        void Nameserver_CircleOffline(object sender, CircleEventArgs e)
        {
            Trace.WriteLine("Circle go offline: " + e.Circle.ToString());
            RefreshCircleList(sender, e);
        }

        void ContactService_ExitCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void ContactService_JoinedCircle(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
            messenger.ContactService.ExitCircle(e.Circle); //Demostrate how to leave a circle.
        }


        void ContactService_CircleMemberLeft(object sender, CircleMemberEventArgs e)
        {
            RefreshCircleList(sender, null);
        }

        void ContactService_CircleMemberJoined(object sender, CircleMemberEventArgs e)
        {
            RefreshCircleList(sender, null);
        }

        void ContactService_JoinCircleInvitationReceived(object sender, JoinCircleInvitationEventArgs e)
        {
            messenger.ContactService.AcceptCircleInvitation(e.Circle);
        }

        void ContactService_CircleCreated(object sender, CircleEventArgs e)
        {
            RefreshCircleList(sender, e);
        }

        void RefreshCircleList(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(RefreshCircleList), sender, e);
                return;
            }

            Contact circle = null;

            if (e is CircleMemberEventArgs)
                circle = (e as CircleMemberEventArgs).Circle;
            else if (e is CircleEventArgs)
                circle = (e as CircleEventArgs).Circle;

            if (toolStripSortByStatus.Checked)
            {
                SortByStatus(circle);
            }
            else
            {
                SortByGroup(circle);
            }
        }

        void ServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Method + ": " + e.Exception.ToString(), sender.GetType().Name); 
        }

        void ContactService_SynchronizationCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(ContactService_SynchronizationCompleted), sender, e);
                return;
            }

            lblNews.Text = "Getting your friends' news...";
            messenger.WhatsUpService.GetWhatsUp(200);
        }

        List<ActivityDetailsType> activities = new List<ActivityDetailsType>();
        void WhatsUpService_GetWhatsUpCompleted(object sender, GetWhatsUpCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<GetWhatsUpCompletedEventArgs>(WhatsUpService_GetWhatsUpCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                lblNews.Text = "ERROR: " + e.Error.ToString();
            }
            else
            {
                activities.Clear();

                foreach (ActivityDetailsType activityDetails in e.Response.Activities)
                {
                    // Show status news
                    if (activityDetails.ApplicationId == "6262816084389410")
                    {
                        activities.Add(activityDetails);
                    }

                    Contact c = messenger.ContactList.GetContactByCID(long.Parse(activityDetails.OwnerCID));

                    if (c != null)
                    {                        
                        c.Activities.Add(activityDetails);                        
                    }
                }

                if (activities.Count == 0)
                {
                    lblNews.Text = "No news";
                    return;
                }

                lblNewsLink.Text = "Get Feeds";
                lblNewsLink.Tag = e.Response.FeedUrl;
                tmrNews.Enabled = true;
            }
        }

        private int currentActivity = 0;
        private bool activityForward = true;
        private void tmrNews_Tick(object sender, EventArgs e)
        {
            if (currentActivity >= activities.Count || currentActivity < 0)
            {
                currentActivity = 0;
            }

            ActivityDetailsType activitiy = activities[currentActivity];
            if (activitiy.ApplicationId == "6262816084389410")
            {
                string name = string.Empty;
                string status = string.Empty;

                foreach (TemplateVariableBaseType t in activitiy.TemplateVariables)
                {
                    if (t is PublisherIdTemplateVariable)
                    {
                        name = ((PublisherIdTemplateVariable)t).NameHint;
                    }
                    else if (t is TextTemplateVariable)
                    {
                        status = ((TextTemplateVariable)t).Value;
                    }
                }

                lblNews.Text = name + ": " + status;

                Contact c = messenger.ContactList.GetContactByCID(long.Parse(activitiy.OwnerCID));

                if (c != null)
                {
                    if (c.DisplayImage != null && c.DisplayImage.Image != null)
                    {
                        pbNewsPicture.Image = c.DisplayImage.Image;
                    }
                    else if (c.UserTile != null)
                    {
                        pbNewsPicture.LoadAsync(c.UserTile.AbsoluteUri);
                    }
                    else
                    {
                        pbNewsPicture.Image = null;
                    }
                }
            }
            if (activityForward)
                currentActivity++;
            else
                currentActivity--;
        }

        private void cmdPrev_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = false;

                if (currentActivity > 0)
                    currentActivity--;
                else
                    currentActivity = activities.Count - 1;

                if (tmrNews.Enabled)
                    tmrNews_Tick(this, EventArgs.Empty);
            }
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            if (activities.Count > 0)
            {
                activityForward = true;

                if (currentActivity < activities.Count)
                    currentActivity++;

                if (tmrNews.Enabled)
                    tmrNews_Tick(this, EventArgs.Empty);
            }
        }

        private void lblNewsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lblNewsLink.Tag != null)
            {
                Process.Start(lblNewsLink.Tag.ToString());
            }
        }

        void Owner_PersonalMessageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(Owner_PersonalMessageChanged), sender, e);
                return;
            }

            lblName.Text = Messenger.ContactList.Owner.Name;

            if (Messenger.ContactList.Owner.PersonalMessage != null && Messenger.ContactList.Owner.PersonalMessage.Message != null)
            {
                lblPM.Text = System.Web.HttpUtility.HtmlDecode(Messenger.ContactList.Owner.PersonalMessage.Message);
            }
        }

        void Owner_DisplayImageChanged(object sender, DisplayImageChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<DisplayImageChangedEventArgs>(Owner_DisplayImageChanged), sender, e);
                return;
            }

            displayImageBox.Image = e.NewDisplayImage.Image;
        }
        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientForm());
        }

        void Nameserver_ContactOnline(object sender, ContactEventArgs e)
        {
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOffline), sender, e);
        }
        void Nameserver_ContactOffline(object sender, ContactEventArgs e)
        {
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOffline), sender, e);
        }

        void ContactOnlineOffline(object sender, ContactEventArgs e)
        {
            if (toolStripSortByStatus.Checked)
                SortByStatus(e.Contact);
            else
                SortByGroup(e.Contact);
        }

        void Nameserver_PingAnswer(object sender, PingAnswerEventArgs e)
        {
            nextPing = e.SecondsToWait;
        }

        void Nameserver_OIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived), sender, e);
                return;
            }

            if (DialogResult.Yes == MessageBox.Show(
                "OIM received at : " + e.ReceivedTime + "\r\nFrom : " + e.NickName + " (" + e.Email + ") " + ":\r\n"
                + e.Message + "\r\n\r\n\r\nClick yes, if you want to receive this message next time you login.",
                "Offline Message from " + e.Email, MessageBoxButtons.YesNoCancel))
            {
                e.IsRead = false;
            }
        }

        void OIMService_OIMSendCompleted(object sender, OIMSendCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "OIM Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Nameserver_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded), sender, e);
                return;
            }

            Contact contact = e.Contact;
            if (messenger.ContactList.Owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd
                /* || messenger.Nameserver.BotMode */)  //If you want your provisioned account in botmode to fire ReverseAdded event, uncomment this.
            {
                // Show pending window if it is necessary.
                if (contact.OnPendingList || 
                    (contact.OnReverseList && !contact.OnAllowedList && !contact.OnBlockedList && !contact.OnPendingList))
                {
                    ReverseAddedForm form = new ReverseAddedForm(contact);
                    form.FormClosed += delegate(object f, FormClosedEventArgs fce)
                    {
                        form = f as ReverseAddedForm;
                        if (DialogResult.OK == form.DialogResult)
                        {
                            if (form.AddToContactList)
                            {
                                messenger.ContactService.AddNewContact(contact.Mail);
                                System.Threading.Thread.Sleep(200);

                                if (form.Blocked)
                                {
                                    contact.Blocked = true;
                                }
                            }
                            else if (form.Blocked)
                            {
                                contact.Blocked = true;
                            }
                            else
                            {
                                contact.OnAllowedList = true;
                            }

                            System.Threading.Thread.Sleep(200);
                            contact.OnPendingList = false;
                        }
                        return;
                    };
                    form.Show(this);
                }
                else
                {
                    MessageBox.Show(contact.Mail + " accepted your invitation and added you their contact list.");
                }
            }
        }


        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate void SetStatusDelegate(string status);

        private void SetStatusSynchronized(string status)
        {
            statusBar.Text = status;
        }

        private void SetStatus(string status)
        {
            if (InvokeRequired)
            {
                this.Invoke(new SetStatusDelegate(SetStatusSynchronized), new object[] { status });
            }
            else
            {
                SetStatusSynchronized(status);
            }
        }

        /// <summary>
        /// Sign into the messenger network. Disconnect first if a connection has already been established.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginButton_Click(object sender, System.EventArgs e)
        {
            switch (Convert.ToInt32(loginButton.Tag))
            {
                case 0: // not connected -> connect
                    {
                        if (messenger.Connected)
                        {
                            SetStatus("Disconnecting from server");
                            messenger.Disconnect();
                        }

                        // set the credentials, this is ofcourse something every MSNPSharp program will need to implement.
                        messenger.Credentials = new Credentials(accountTextBox.Text, passwordTextBox.Text, (MsnProtocol)Enum.Parse(typeof(MsnProtocol), comboProtocol.Text));
                       
                        // inform the user what is happening and try to connecto to the messenger network.
                        SetStatus("Connecting to server");
                        messenger.Connect();

                        displayImageBox.Image = global::MSNPSharpClient.Properties.Resources.loading;

                        loginButton.Tag = 1;
                        loginButton.Text = "Cancel";
                        initialExpand = true;

                        // note that Messenger.Connect() will run in a seperate thread and return immediately.
                        // it will fire events that informs you about the status of the connection attempt. 
                        // these events are registered in the constructor.

                    }
                    break;

                case 1: // connecting -> cancel
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus(null);
                        else
                            SortByGroup(null);

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = false;
                        comboPlaces.Visible = false;
                        initialExpand = true;

                    }
                    break;

                case 2: // connected -> disconnect
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus(null);
                        else
                            SortByGroup(null);

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = true;
                        comboPlaces.Visible = true;
                        initialExpand = true;

                    }
                    break;
            }
        }

        private void login_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == '\r') || (e.KeyChar == '\r'))
            {
                loginButton.PerformClick();
            }
        }

        void Owner_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged), sender, e);
                return;
            }

            if (messenger.Nameserver.IsSignedIn)
            {
                comboStatus.SelectedIndex = comboStatus.FindString(GetStatusString(Messenger.ContactList.Owner.Status));
            }
        }

        private string GetStatusString(PresenceStatus status)
        {
            switch (status)
            {
                case PresenceStatus.Away:
                case PresenceStatus.BRB:
                case PresenceStatus.Lunch:
                case PresenceStatus.Idle:
                    return "Away";
                case PresenceStatus.Online:
                    return "Online";
                case PresenceStatus.Offline:
                    return "Offline";
                case PresenceStatus.Hidden:
                    return "Hidden";
                case PresenceStatus.Busy:
                case PresenceStatus.Phone:
                    return "Busy";

            }

            return "Offline";
        }

        private void comboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(comboStatus_SelectedIndexChanged), sender, e);
                return;
            }

            PresenceStatus newstatus = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            if (messenger.Connected)
            {
                if (newstatus == PresenceStatus.Offline)
                {
                    PresenceStatus old = Messenger.ContactList.Owner.Status;

                    foreach (ConversationForm convform in ConversationForms)
                    {
                        if (convform.Visible == true)
                        {
                            if (MessageBox.Show("You are signing out from example client. All windows will be closed.", "Sign out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            {
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Messenger.Disconnect();
                }
                else
                {
                    Messenger.ContactList.Owner.Status = newstatus;
                }
            }
            else if (newstatus == PresenceStatus.Offline)
            {
                comboStatus.SelectedIndex = 0;
            }
        }

        private void comboStatus_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            e.Graphics.FillRectangle(Brushes.White, e.Bounds);
            if ((e.State & DrawItemState.Selected) != DrawItemState.None)
                e.DrawBackground();

            PresenceStatus newstatus = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Items[e.Index].ToString());
            Brush brush = Brushes.Green;

            switch (newstatus)
            {
                case PresenceStatus.Online:
                    brush = Brushes.Green;
                    break;

                case PresenceStatus.Busy:
                    brush = Brushes.Red;
                    break;

                case PresenceStatus.Away:
                    brush = Brushes.Orange;
                    break;

                case PresenceStatus.Hidden:
                    brush = Brushes.Gray;
                    break;

                case PresenceStatus.Offline:
                    brush = Brushes.Black;
                    break;
            }

            Point imageLocation = new Point(e.Bounds.X + 2, e.Bounds.Y + 2);
            e.Graphics.FillRectangle(brush, new Rectangle(imageLocation, new Size(12, 12)));

            PointF textLocation = new PointF(imageLocation.X + 16, imageLocation.Y);
            e.Graphics.DrawString(newstatus.ToString(), PARENT_NODE_FONT, Brushes.Black, textLocation);
        }

        private void comboPlaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPlaces.SelectedIndex > 0)
            {
                string place = comboPlaces.Text.Split(' ')[comboPlaces.Text.Split(' ').Length - 1];
                if (comboPlaces.SelectedIndex == 1)
                {
                    Messenger.ContactList.Owner.Status = PresenceStatus.Offline;
                    comboPlaces.Visible = false;
                }
                else if (comboPlaces.SelectedIndex == comboPlaces.Items.Count - 1)
                {
                    Messenger.ContactList.Owner.SignoutFromEverywhere();
                    comboPlaces.Visible = false;
                }
                else if (comboPlaces.SelectedIndex >= 2)
                {

                    Messenger.ContactList.Owner.SignoutFrom(places[comboPlaces.SelectedIndex - 2]);  //places does not contain the current places.
                }
            }
        }

        void cbRobotMode_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox cbBotMode = sender as ComboBox;
            messenger.Nameserver.BotMode = cbRobotMode.Checked;
        }

        List<Guid> places = new List<Guid>(0);

        private void Owner_PlacesChanged(object sender, EventArgs e)
        {
            if (comboPlaces.InvokeRequired)
            {
                comboPlaces.Invoke(new EventHandler(Owner_PlacesChanged), sender, e);
                return;
            }

            // if (Messenger.ContactList.Owner.Places.Count > 1)
            {
                comboPlaces.BeginUpdate();
                comboPlaces.Items.Clear();
                comboPlaces.Items.Add("(" + Messenger.ContactList.Owner.PlaceCount + ") Places");
                comboPlaces.Items.Add("Signout from here (" + Messenger.ContactList.Owner.EpName + ")");

                foreach (KeyValuePair<Guid, EndPointData> keyvalue in Messenger.ContactList.Owner.EndPointData)
                {
                    if (keyvalue.Key != NSMessageHandler.MachineGuid)
                    {
                        comboPlaces.Items.Add("Signout from " + (keyvalue.Value as PrivateEndPointData).Name);
                        places.Add(keyvalue.Key);
                    }
                }

                comboPlaces.Items.Add("Signout from everywhere");
                comboPlaces.SelectedIndex = 0;
                comboPlaces.Visible = true;
                comboPlaces.EndUpdate();
            }
        }

        private void NameserverProcessor_ConnectionEstablished(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(NameserverProcessor_ConnectionEstablished), sender, e);
                return;
            }

            messenger.Nameserver.AutoSynchronize = !cbRobotMode.Checked;

            SetStatus("Connected to server");
        }

        private void Nameserver_SignedIn(object sender, EventArgs e)
        {

            if (InvokeRequired)
            {
                Invoke(new EventHandler(Nameserver_SignedIn), sender, e);
                return;
            }

            SetStatus("Signed into the messenger network as " + Messenger.ContactList.Owner.Name);

            // set our presence status
            loginButton.Tag = 2;
            loginButton.Text = "Sign off";
            pnlNameAndPM.Visible = true;
            comboPlaces.Visible = true;

            Messenger.ContactList.Owner.DisplayImageChanged += new EventHandler<DisplayImageChangedEventArgs>(Owner_DisplayImageChanged);
            Messenger.ContactList.Owner.PersonalMessageChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            Messenger.ContactList.Owner.ScreenNameChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            Messenger.ContactList.Owner.PlacesChanged += new EventHandler<PlaceChangedEventArgs>(Owner_PlacesChanged);
            Messenger.ContactList.Owner.StatusChanged += new EventHandler<StatusChangedEventArgs>(Owner_StatusChanged);
            Messenger.ContactList.Owner.Status = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            propertyGrid.SelectedObject = Messenger.ContactList.Owner;
            displayImageBox.Image = Messenger.ContactList.Owner.DisplayImage.Image;

            Invoke(new EventHandler<EventArgs>(UpdateContactlist), sender, e);
        }

        private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff), sender, e);
                return;
            }

            SetStatus("Signed off from the messenger network");
            ResetAll();
        }

        private void ResetAll()
        {
            tmrKeepOnLine.Enabled = false;
            tmrNews.Enabled = false;

            displayImageBox.Image = null;
            loginButton.Tag = 0;
            loginButton.Text = "> Sign in";
            pnlNameAndPM.Visible = false;
            comboPlaces.Visible = false;
            propertyGrid.SelectedObject = null;

            treeViewFavoriteList.Nodes.Clear();
            treeViewFilterList.Nodes.Clear();

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            places.Clear();

            List<ConversationForm> convFormsClone = new List<ConversationForm>(ConversationForms);
            foreach(ConversationForm convForm in convFormsClone)
            {
                convForm.Close();
            }

            YIMMessageForms.Clear();
        }

        private void Nameserver_ExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred), new object[] { sender, e });
            }
            else
            {

                // ignore the unauthorized exception, since we're handling that error in another method.
                if (e.Exception is UnauthorizedException)
                    return;

                MessageBox.Show(e.Exception.ToString(), "Nameserver exception");
            }
        }

        private void NameserverProcessor_ConnectingException(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Connecting exception");
            SetStatus("Connecting failed");
        }

        private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show("Authentication failed, check your account or password.", e.Exception.InnerException.Message);
            SetStatus("Authentication failed");
        }

        /// <summary>
        /// Updates the treeView.
        /// </summary>
        private void UpdateContactlist(object sender, EventArgs e)
        {
            if (messenger.Connected == false)
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus(null);
            else
                SortByGroup(null);

            tmrKeepOnLine.Enabled = true;
        }


        /// <summary>
        /// Notifies the user of errors which are send by the MSN server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Nameserver_ServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            // when the MSN server sends an error code we want to be notified.
            MessageBox.Show(e.MSNError.ToString(), "Server error received");
            SetStatus("Server error received");
        }

        private void messenger_ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            // use the invoke method to create the form in the main thread, ONLY create the form after a contact joined our conversation.
            e.Conversation.ContactJoined += new EventHandler<ContactConversationEventArgs>(Conversation_ContactJoined);
        }

        void Conversation_ContactJoined(object sender, ContactEventArgs e)
        {
            //The request is initiated by remote user, so we needn't invite anyone.
            this.Invoke(new CreateConversationDelegate(CreateConversationForm), new object[] { sender, e.Contact });
            Conversation convers = sender as Conversation;
            convers.ContactJoined -= Conversation_ContactJoined; //We don't care any further join event anymore.
        }

        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate ConversationForm CreateConversationDelegate(Conversation conversation, Contact remote);

        private ConversationForm CreateConversationForm(Conversation conversation, Contact remote)
        {
            foreach (ConversationForm cform in ConversationForms)
            {
                if (cform.CanAttach(conversation))
                {
                    cform.AttachConversation(conversation);
                    return cform;
                }
            }

            // create a new conversation. However do not show the window untill a message is received.
            // for example, a conversation will be created when the remote client sends wants to send
            // you a file. You don't want to show the conversation form in that case.
            ConversationForm conversationForm = new ConversationForm(conversation, Messenger, remote);
            // do this to create the window handle. Otherwise we are not able to call Invoke() on the
            // conversation form later.
            conversationForm.Handle.ToInt32();
            ConversationForms.Add(conversationForm);

            conversationForm.FormClosing += delegate
            {
                ConversationForms.Remove(conversationForm);
            };

            return conversationForm;
        }

        private delegate DialogResult ShowFileDialogDelegate(FileDialog dialog);

        private DialogResult ShowFileDialog(FileDialog dialog)
        {
            return dialog.ShowDialog();
        }

        /// <summary>
        /// Asks the user to accept or deny the incoming filetransfer invitation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messenger_TransferInvitationReceived(object sender, MSNSLPInvitationEventArgs e)
        {
            if (e.TransferProperties.DataType == DataTransferType.File)
            {
                if (MessageBox.Show(
                    e.TransferProperties.RemoteContact.Name +
                    " wants to send you a file.\r\nFilename: " +
                    e.Filename + "\r\nLength (bytes): " + e.FileSize,
                    "Filetransfer invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // by setting the Accept property in the EventArgs to true we give the transfer a green light				
                    saveFileDialog.FileName = e.Filename;
                    if ((DialogResult)Invoke(new ShowFileDialogDelegate(ShowFileDialog), new object[] { saveFileDialog }) == DialogResult.OK)
                    {
                        e.TransferSession.DataStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                        //e.Handler.AcceptTransfer (e);
                        e.Accept = true;
                        e.TransferSession.AutoCloseStream = true;
                    }
                }
            }
            else if (e.TransferProperties.DataType == DataTransferType.Activity)
            {
                if (MessageBox.Show(
                    e.TransferProperties.RemoteContact.Name +
                    " wants to invite you to join an activity.\r\nActivity name: " +
                    e.Activity.ActivityName + "\r\nAppID: " + e.Activity.AppID,
                    "Activity invitation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    e.TransferSession.DataStream = new MemoryStream();
                    e.Accept = true;
                    e.TransferSession.AutoCloseStream = true;
                }
            }
        }

        private int nextPing = 50;
        private void tmrKeepOnLine_Tick(object sender, EventArgs e)
        {
            if (nextPing > 0)
                nextPing--;
            if (nextPing == 0)
            {
                nextPing--;
                messenger.Nameserver.SendPing();
            }
        }



        private static Font PARENT_NODE_FONT = new Font("Tahoma", 8f, FontStyle.Bold);
        private static Font USER_NODE_FONT = new Font("Tahoma", 8f);
        private static Font USER_NODE_FONT_BANNED = new Font("Tahoma", 8f, FontStyle.Strikeout);
        public class StatusSorter : IComparer
        {
            public static StatusSorter Default = new StatusSorter();
            private StatusSorter()
            {
            }
            public int Compare(object x, object y)
            {
                TreeNode node = x as TreeNode;
                TreeNode node2 = y as TreeNode;

                if (node.Tag is string && node2.Tag is string)
                {
                    // Online (0), Offline (1)
                    return node.Tag.ToString().CompareTo(node2.Tag.ToString());
                }
                else if (node.Tag is Circle && node2.Tag is Circle)
                {

                    return ((Circle)node.Tag).AddressBookId.CompareTo(((Circle)node2.Tag).AddressBookId);

                }
                else if (node.Tag is Contact && node2.Tag is Contact)
                {
                    if (((Contact)node.Tag).Online == ((Contact)node2.Tag).Online)
                    {
                        return string.Compare(((Contact)node.Tag).Name, ((Contact)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (((Contact)node.Tag).Online)
                        return -1;
                    else if (((Contact)node2.Tag).Online)
                        return 1;
                }
                else if (node.Tag is ContactGroup && node2.Tag is ContactGroup)
                {
                    return string.Compare(((ContactGroup)node.Tag).Name, ((ContactGroup)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                }
                return 0;
            }
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) && (e.Node.Level != 0))
            {
                Contact selectedContact = treeViewFavoriteList.SelectedNode.Tag as Contact;

                if (selectedContact != null)
                {
                    propertyGrid.SelectedObject = selectedContact;

                    if (selectedContact.Online && (!(selectedContact is Circle)))
                    {
                        sendIMMenuItem.PerformClick();
                    }
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Node.Level == 0)
                {
                    if (e.Node.IsExpanded || (e.Node.ImageIndex == ImageIndexes.Open))
                    {
                        e.Node.Collapse();
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = (e.Node.Tag is Circle) ? ImageIndexes.Circle : ImageIndexes.Closed;
                    }
                    else if (e.Node.Nodes.Count > 0)
                    {
                        e.Node.Expand();
                        e.Node.ImageIndex = e.Node.SelectedImageIndex = (e.Node.Tag is Circle) ? ImageIndexes.Circle : ImageIndexes.Open;
                    }
                    if (e.Node.Tag is ContactGroup || e.Node.Tag is Circle)
                    {
                        propertyGrid.SelectedObject = e.Node.Tag;
                    }
                }
                else
                {
                    Contact selectedContact = (Contact)e.Node.Tag;

                    if (selectedContact is Circle)
                    {
                        if (e.Node.IsExpanded)
                        {
                            e.Node.Collapse();
                        }
                        else
                        {
                            e.Node.Expand();
                        }
                    }

                    propertyGrid.SelectedObject = selectedContact;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Tag is Contact && e.Node.Level > 0)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;
                    Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;

                    if (contact.Blocked)
                    {
                        blockMenuItem.Visible = false;
                        unblockMenuItem.Visible = true;
                    }
                    else
                    {
                        blockMenuItem.Visible = true;
                        unblockMenuItem.Visible = false;
                    }

                    if (contact.Online)
                    {
                        sendIMMenuItem.Visible = true;
                        sendOIMMenuItem.Visible = false;

                        toolStripMenuItem2.Visible = true;
                    }
                    else
                    {
                        sendIMMenuItem.Visible = false;
                        sendOIMMenuItem.Visible = true;

                        toolStripMenuItem2.Visible = false;
                    }

                    deleteMenuItem.Visible = contact.Guid != Guid.Empty;

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    userMenuStrip.Show(point.X - userMenuStrip.Width, point.Y);
                }
                else if (e.Node.Tag is ContactGroup)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    groupContextMenu.Show(point.X - groupContextMenu.Width, point.Y);
                }
            }
        }

        private void blockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = true;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT_BANNED;
        }

        private void unblockMenuItem_Click(object sender, EventArgs e)
        {
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = false;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT;
        }

        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            RemoveContactForm form = new RemoveContactForm();
            form.FormClosed += delegate(object f, FormClosedEventArgs fce)
            {
                form = f as RemoveContactForm;
                if (DialogResult.OK == form.DialogResult)
                {
                    if (form.Block)
                    {
                        contact.Blocked = true;
                    }

                    if (form.RemoveFromAddressBook)
                    {
                        messenger.ContactService.RemoveContact(contact);
                    }
                    else
                    {
                        contact.IsMessengerUser = false;
                    }
                }
            };
            form.ShowDialog(this);
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = treeViewFavoriteList.SelectedNode.Tag as Contact;
            if (!contact.IsMessengerUser) return;

            if (!contact.OnForwardList)
            {
                AddContactForm acf = new AddContactForm(contact.Mail);

                if (DialogResult.OK == acf.ShowDialog(this) &&
                    acf.Account != String.Empty)
                {
                    messenger.ContactService.AddNewContact(acf.Account, acf.InvitationMessage);
                }

                return;
            }

            bool activate = false;
            ConversationForm activeForm = null;

            if (contact.ClientType != ClientType.EmailMember)
            {
                foreach (ConversationForm conv in ConversationForms)
                {
                    if (conv.ActiveConversation.HasContact(contact))
                    {
                        activeForm = conv;
                        activate = true;
                    }

                }
            }
            else
            {
                if (YIMMessageForms.ContainsKey(contact.Mail.ToLowerInvariant()))
                {
                    activeForm = YIMMessageForms[contact.Mail.ToLowerInvariant()];
                    activate = true;
                }
            }

            if (activate)
            {
                if (activeForm.WindowState == FormWindowState.Minimized || activeForm.Visible == false)
                    activeForm.Show();

                activeForm.Activate();
                return;
            }

            ConversationForm form = null;
            if (contact.ClientType != ClientType.EmailMember)
            {
                Conversation convers = messenger.CreateConversation();
                convers.Invite(contact);
                form = CreateConversationForm(convers, contact);
            }
            else
            {
                form = new ConversationForm(Messenger, contact);
                YIMMessageForms[contact.Mail.ToLowerInvariant()] = form;
            }

            form.Show();
        }

        private void sendOfflineMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;
            messenger.OIMService.SendOIMMessage(selectedContact, new TextMessage("MSNP offline message"));

        }

        private void sendMIMMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            if (selectedContact.MobileAccess || selectedContact.ClientType == ClientType.PhoneMember)
            {
                messenger.Nameserver.SendMobileMessage(selectedContact, "MSNP mobile message");
            }
            else
                MessageBox.Show("This contact is not able to receive mobile messages");
        }


        private void btnSortBy_Click(object sender, EventArgs e)
        {
            int x = ((base.Location.X + ListPanel.Location.X) + treeViewFavoriteList.Location.X) + 5;
            int y = ((base.Location.Y + ListPanel.Location.Y) + treeViewFavoriteList.Location.Y) + (3 * btnSortBy.Height);
            sortContextMenu.Show(x, y);
            sortContextMenu.Focus();
        }

        private void toolStripSortByStatus_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortByStatus.Checked)
            {
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.NoGroupNodeKey);
                foreach (ContactGroup cg in messenger.ContactGroups)
                {
                    treeViewFavoriteList.Nodes.RemoveByKey(cg.Guid);
                }

                SortByStatus(null);
            }
            else
            {
                this.toolStripSortByStatus.Checked = true;
            }
        }

        private bool initialExpand = true;

        private string GetCircleDisplayName(Circle circle)
        {
            if (circle == null)
                return string.Empty;

            return circle.Name + " (" + circle.ContactList.Values.Count.ToString() + " members)";
        }


        private void SortByFavAndCircle(Contact contactToUpdate)
        {
            TreeNode favoritesNode = null; // (0/0)
            TreeNode circlesNode = null; // (0/0)

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.FavoritesNodeKey))
            {
                favoritesNode = treeViewFavoriteList.Nodes[ImageIndexes.FavoritesNodeKey];
            }
            else
            {
                favoritesNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.FavoritesNodeKey, "Favorites", ImageIndexes.Closed, ImageIndexes.Closed);
                favoritesNode.NodeFont = PARENT_NODE_FONT;
                favoritesNode.Tag = ImageIndexes.FavoritesNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.CircleNodeKey))
            {
                circlesNode = treeViewFavoriteList.Nodes[ImageIndexes.CircleNodeKey];
            }
            else
            {
                circlesNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.CircleNodeKey, "Circles", ImageIndexes.Closed, ImageIndexes.Closed);
                circlesNode.NodeFont = PARENT_NODE_FONT;
                circlesNode.Tag = ImageIndexes.CircleNodeKey;
            }

            if (contactToUpdate == null)
            {
                // Initial sort
                favoritesNode.Nodes.Clear();
                circlesNode.Nodes.Clear();

                foreach (Circle circle in Messenger.CircleList)
                {
                    TreeNode circleNode = circlesNode.Nodes.Add(circle.Hash, GetCircleDisplayName(circle), ImageIndexes.Circle, ImageIndexes.Circle);
                    circleNode.NodeFont = PARENT_NODE_FONT;
                    circleNode.Tag = circle;

                    foreach (Contact contact in circle.ContactList.All)
                    {
                        // Get real passport contact to chat with... If this contact isn't on our forward list, show add contact form...
                        string text = contact.Name;
                        if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                        {
                            text += " - " + contact.PersonalMessage.Message;
                        }
                        if (contact.Name != contact.Mail)
                        {
                            text += " (" + contact.Mail + ")";
                        }

                        TreeNode newnode = circleNode.Nodes.Add(contact.Hash, text);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                        newnode.Tag = contact;
                    }
                }

                ContactGroup favGroup = messenger.ContactGroups.FavoriteGroup;
                if (favGroup != null)
                {
                    foreach (Contact c in messenger.ContactList.Forward)
                    {
                        if (c.HasGroup(favGroup))
                        {
                            string text = c.Name;
                            if (c.PersonalMessage != null && !String.IsNullOrEmpty(c.PersonalMessage.Message))
                            {
                                text += " - " + c.PersonalMessage.Message;
                            }
                            if (c.Name != c.Mail)
                            {
                                text += " (" + c.Mail + ")";
                            }

                            TreeNode newnode = favoritesNode.Nodes.ContainsKey(c.Hash) ?
                                favoritesNode.Nodes[c.Hash] : favoritesNode.Nodes.Add(c.Hash, text);

                            newnode.NodeFont = c.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(c.Status);
                            newnode.Tag = c;
                        }
                    }
                }
            }
            else if (contactToUpdate is Circle)
            {
                // Circle event
                Circle circle = contactToUpdate as Circle;
                bool isDeleted = (Messenger.CircleList[circle.AddressBookId, circle.HostDomain] == null);

                if (!isDeleted)
                {
                    TreeNode circlenode = circlesNode.Nodes.ContainsKey(circle.Hash) ?
                        circlesNode.Nodes[circle.Hash] : circlesNode.Nodes.Add(circle.Hash, GetCircleDisplayName(circle), ImageIndexes.Circle, ImageIndexes.Circle);

                    circlenode.NodeFont = PARENT_NODE_FONT;
                    circlenode.Tag = circle;

                    foreach (Contact contact in circle.ContactList.All)
                    {
                        // Get real passport contact to chat with... If this contact isn't on our forward list, show add contact form...
                        string text2 = contact.Name;
                        if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                        {
                            text2 += " - " + contact.PersonalMessage.Message;
                        }
                        if (contact.Name != contact.Mail)
                        {
                            text2 += " (" + contact.Mail + ")";
                        }

                        TreeNode newnode = circlenode.Nodes.ContainsKey(contact.Hash) ?
                            circlenode.Nodes[contact.Hash] : circlenode.Nodes.Add(contact.Hash, text2);

                        newnode.Text = text2;
                        newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                    }

                    circlenode.Text = GetCircleDisplayName(circle);
                }
                else
                {
                    circlesNode.Nodes.RemoveByKey(circle.Hash);
                }
            }
            else
            {
                // Contact event... Contact is not null.
                // Favorite
                ContactGroup favGroup = messenger.ContactGroups.FavoriteGroup;
                if (favGroup != null && contactToUpdate.HasGroup(favGroup))
                {
                    Contact contact = messenger.ContactList[contactToUpdate.Mail, contactToUpdate.ClientType];
                    string text = contact.Name;
                    if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                    {
                        text += " - " + contact.PersonalMessage.Message;
                    }
                    if (contact.Name != contact.Mail)
                    {
                        text += " (" + contact.Mail + ")";
                    }

                    TreeNode contactNode = favoritesNode.Nodes.ContainsKey(contactToUpdate.Hash) ?
                        favoritesNode.Nodes[contactToUpdate.Hash] : favoritesNode.Nodes.Add(contactToUpdate.Hash, text);

                    contactNode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    contactNode.ImageIndex = contactNode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contactToUpdate.Status);
                    contactNode.Tag = contactToUpdate;
                }
            }

            int onlineCount = 0;
            foreach (TreeNode nodeFav in favoritesNode.Nodes)
            {
                if (nodeFav.Tag is Contact && (((Contact)nodeFav.Tag).Online))
                {
                    onlineCount++;
                }
            }
            favoritesNode.Text = "Favorites (" + onlineCount + "/" + favoritesNode.Nodes.Count + ")";

            onlineCount = 0;
            foreach (TreeNode nodeCircle in circlesNode.Nodes)
            {
                if (nodeCircle.Tag is Circle && (((Circle)nodeCircle.Tag).Online))
                {
                    onlineCount++;
                }
            }
            circlesNode.Text = "Circles (" + onlineCount + "/" + circlesNode.Nodes.Count + ")";
        }

        private void SortByStatus(Contact contactToUpdate)
        {
            TreeNode selectedNode = treeViewFavoriteList.SelectedNode;
            bool isExpanded = (selectedNode != null && selectedNode.IsExpanded);

            treeViewFavoriteList.BeginUpdate();
            toolStripSortBygroup.Checked = false;

            SortByFavAndCircle(contactToUpdate);

            TreeNode onlineNode = null; // (0)
            TreeNode mobileNode = null; // (0)
            TreeNode offlineNode = null; // (0)

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.OnlineNodeKey))
            {
                onlineNode = treeViewFavoriteList.Nodes[ImageIndexes.OnlineNodeKey];
            }
            else
            {
                onlineNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.OnlineNodeKey, "Online", ImageIndexes.Closed, ImageIndexes.Closed);
                onlineNode.NodeFont = PARENT_NODE_FONT;
                onlineNode.Tag = ImageIndexes.OnlineNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.MobileNodeKey))
            {
                mobileNode = treeViewFavoriteList.Nodes[ImageIndexes.MobileNodeKey];
            }
            else
            {
                mobileNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.MobileNodeKey, "Mobile", ImageIndexes.Closed, ImageIndexes.Closed);
                mobileNode.NodeFont = PARENT_NODE_FONT;
                mobileNode.Tag = ImageIndexes.MobileNodeKey;
            }

            if (treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.OfflineNodeKey))
            {
                offlineNode = treeViewFavoriteList.Nodes[ImageIndexes.OfflineNodeKey];
            }
            else
            {
                offlineNode = treeViewFavoriteList.Nodes.Add(ImageIndexes.OfflineNodeKey, "Offline", ImageIndexes.Closed, ImageIndexes.Closed);
                offlineNode.NodeFont = PARENT_NODE_FONT;
                offlineNode.Tag = ImageIndexes.OfflineNodeKey;
            }

            // Re-sort all
            if (contactToUpdate == null)
            {
                mobileNode.Nodes.Clear();
                onlineNode.Nodes.Clear();
                offlineNode.Nodes.Clear();

                foreach (Contact contact in messenger.ContactList.All)
                {
                    string text = contact.Name;
                    if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                    {
                        text += " - " + contact.PersonalMessage.Message;
                    }
                    if (contact.Name != contact.Mail)
                    {
                        text += " (" + contact.Mail + ")";
                    }

                    TreeNode newnode = contact.Online ? onlineNode.Nodes.Add(contact.Hash, text) : offlineNode.Nodes.Add(contact.Hash, text);
                    newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                    newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;

                    if (contact.MobileAccess || contact.ClientType == ClientType.PhoneMember)
                    {
                        TreeNode newnode2 = mobileNode.Nodes.Add(contact.Hash, text);
                        newnode2.ImageIndex = newnode2.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                        newnode2.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode2.Tag = contact;
                    }
                }
            }
            else if ((contactToUpdate is Circle) == false)
            {
                TreeNode contactNode = null;

                if (contactToUpdate.Online)
                {
                    if (offlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        offlineNode.Nodes.RemoveByKey(contactToUpdate.Hash);
                    }
                    if (onlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        contactNode = onlineNode.Nodes[contactToUpdate.Hash];
                    }
                }
                else
                {
                    if (onlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        onlineNode.Nodes.RemoveByKey(contactToUpdate.Hash);
                    }
                    if (offlineNode.Nodes.ContainsKey(contactToUpdate.Hash))
                    {
                        contactNode = offlineNode.Nodes[contactToUpdate.Hash];
                    }
                }

                string text = contactToUpdate.Name;
                if (contactToUpdate.PersonalMessage != null && !String.IsNullOrEmpty(contactToUpdate.PersonalMessage.Message))
                {
                    text += " - " + contactToUpdate.PersonalMessage.Message;
                }
                if (contactToUpdate.Name != contactToUpdate.Mail)
                {
                    text += " (" + contactToUpdate.Mail + ")";
                }

                if (contactNode == null)
                {
                    contactNode = contactToUpdate.Online ? onlineNode.Nodes.Add(contactToUpdate.Hash, text) : offlineNode.Nodes.Add(contactToUpdate.Hash, text);
                }

                contactNode.Text = text;
                contactNode.ImageIndex = contactNode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contactToUpdate.Status);
                contactNode.NodeFont = contactToUpdate.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                contactNode.Tag = contactToUpdate;

                if (contactToUpdate.MobileAccess || contactToUpdate.ClientType == ClientType.PhoneMember)
                {
                    TreeNode newnode2 = mobileNode.Nodes.ContainsKey(contactToUpdate.Hash) ?
                        mobileNode.Nodes[contactToUpdate.Hash] : mobileNode.Nodes.Add(contactToUpdate.Hash, text);

                    newnode2.ImageIndex = newnode2.SelectedImageIndex = ImageIndexes.GetStatusIndex(contactToUpdate.Status);
                    newnode2.NodeFont = contactToUpdate.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode2.Tag = contactToUpdate;
                }
            }

            onlineNode.Text = "Online (" + onlineNode.Nodes.Count.ToString() + ")";
            offlineNode.Text = "Offline (" + offlineNode.Nodes.Count.ToString() + ")";
            mobileNode.Text = "Mobile (" + mobileNode.Nodes.Count.ToString() + ")";

            treeViewFavoriteList.Sort();

            if (selectedNode != null)
            {
                treeViewFavoriteList.SelectedNode = selectedNode;
                if (isExpanded && treeViewFavoriteList.SelectedNode != null)
                {
                    treeViewFavoriteList.SelectedNode.Expand();
                }
            }
            else
            {
                if (initialExpand && onlineNode.Nodes.Count > 0)
                {
                    onlineNode.Expand();
                    onlineNode.ImageIndex = ImageIndexes.Open;

                    initialExpand = false;
                }
            }

            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = false;
        }

        private void toolStripSortBygroup_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortBygroup.Checked)
            {
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.OnlineNodeKey);
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.MobileNodeKey);
                treeViewFavoriteList.Nodes.RemoveByKey(ImageIndexes.OfflineNodeKey);

                SortByGroup(null);
            }
            else
            {
                this.toolStripSortBygroup.Checked = true;
            }
        }

        private void SortByGroup(Contact contactToUpdate)
        {
            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortByStatus.Checked = false;

            SortByFavAndCircle(contactToUpdate);

            foreach (ContactGroup group in this.messenger.ContactGroups)
            {
                if (group.IsFavorite == false)
                {
                    TreeNode node = treeViewFavoriteList.Nodes.ContainsKey(group.Guid) ?
                        treeViewFavoriteList.Nodes[group.Guid] : treeViewFavoriteList.Nodes.Add(group.Guid, group.Name, ImageIndexes.Closed, ImageIndexes.Closed);

                    node.ImageIndex = ImageIndexes.Closed;
                    node.NodeFont = PARENT_NODE_FONT;
                    node.Tag = group;
                    node.Text = "0";
                }
            }

            TreeNode common = treeViewFavoriteList.Nodes.ContainsKey(ImageIndexes.NoGroupNodeKey) ?
                treeViewFavoriteList.Nodes[ImageIndexes.NoGroupNodeKey] : treeViewFavoriteList.Nodes.Add(ImageIndexes.NoGroupNodeKey, "Others", 0, 0);

            common.ImageIndex = ImageIndexes.Closed;
            common.NodeFont = PARENT_NODE_FONT;
            common.Tag = ImageIndexes.NoGroupNodeKey;
            common.Text = "0";

            foreach (Contact contact in messenger.ContactList.All)
            {
                string text = contact.Name;
                if (contact.PersonalMessage != null && !String.IsNullOrEmpty(contact.PersonalMessage.Message))
                {
                    text += " - " + contact.PersonalMessage.Message;
                }
                if (contact.Name != contact.Mail)
                {
                    text += " (" + contact.Mail + ")";
                }

                if (contact.ContactGroups.Count == 0)
                {
                    TreeNode newnode = common.Nodes.ContainsKey(contact.Hash) ? 
                        common.Nodes[contact.Hash] : common.Nodes.Add(contact.Hash, text);

                    newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                    newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;
                    newnode.Text = text;

                    if (contact.Online)
                        common.Text = (Convert.ToInt32(common.Text) + 1).ToString();
                }
                else
                {
                    foreach (ContactGroup group in contact.ContactGroups)
                    {
                        if (group.IsFavorite == false)
                        {
                            TreeNode found = treeViewFavoriteList.Nodes[group.Guid];
                            TreeNode newnode = found.Nodes.Add(contact.Hash, contact.Name);
                            newnode.ImageIndex = newnode.SelectedImageIndex = ImageIndexes.GetStatusIndex(contact.Status);
                            newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                            newnode.Tag = contact;

                            if (contact.Online)
                                found.Text = (Convert.ToInt32(found.Text) + 1).ToString();
                        }
                    }
                }
            }

            foreach (TreeNode nodeGroup in treeViewFavoriteList.Nodes)
            {
                if (nodeGroup.Tag is ContactGroup)
                {
                    nodeGroup.Text = ((ContactGroup)nodeGroup.Tag).Name + "(" + nodeGroup.Text + "/" + nodeGroup.Nodes.Count + ")";
                }
            }

            common.Text = "Others (" + common.Text + "/" + common.Nodes.Count + ")";

            treeViewFavoriteList.Sort();
            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = true;
        }

        private void treeViewFavoriteList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void treeViewFavoriteList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if ((e.Item is TreeNode)/* && (((TreeNode)e.Item).Level > 0)*/)
            {
                base.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void treeViewFavoriteList_DragOver(object sender, DragEventArgs e)
        {
            Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
            TreeNode nodeAt = ((TreeView)sender).GetNodeAt(pt);
            TreeNode data = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
            if (((data.Level == 0) || (data.Parent == nodeAt)) || (nodeAt.Parent == data.Parent))
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void treeViewFavoriteList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode contactNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (contactNode.Level != 0)
                {
                    Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                    TreeNode newgroupNode = (((TreeView)sender).GetNodeAt(pt).Level == 0) ? ((TreeView)sender).GetNodeAt(pt) : ((TreeView)sender).GetNodeAt(pt).Parent;
                    TreeNode oldgroupNode = contactNode.Parent;
                    Contact contact = (Contact)contactNode.Tag;
                    bool flag = true;
                    try
                    {
                        if (newgroupNode.Tag is ContactGroup)
                        {
                            messenger.ContactService.AddContactToGroup(contact, (ContactGroup)newgroupNode.Tag);
                        }
                        if (oldgroupNode.Tag is ContactGroup)
                        {
                            messenger.ContactService.RemoveContactFromGroup(contact, (ContactGroup)oldgroupNode.Tag);
                        }
                    }
                    catch (Exception)
                    {
                        flag = false;
                    }

                    if (flag)
                    {
                        treeViewFavoriteList.BeginUpdate();
                        TreeNode node3 = (TreeNode)contactNode.Clone();
                        newgroupNode.Nodes.Add(node3);
                        contactNode.Remove();
                        treeViewFavoriteList.EndUpdate();

                        newgroupNode.Text = newgroupNode.Text.Split(new char[] { '/' })[0] + "/" + newgroupNode.Nodes.Count + ")";
                        oldgroupNode.Text = oldgroupNode.Text.Split(new char[] { '/' })[0] + "/" + oldgroupNode.Nodes.Count + ")";
                    }
                }
            }
        }

        private void toolStripDeleteGroup_Click(object sender, EventArgs e)
        {
            ContactGroup selectedGroup = (ContactGroup)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedGroup;

            messenger.ContactGroups.Remove(selectedGroup);

            System.Threading.Thread.Sleep(500);
            Application.DoEvents();
            System.Threading.Thread.Sleep(500);

            SortByGroup(null);
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            if (this.loginButton.Tag.ToString() != "2")
            {
                MessageBox.Show("Please sign in first.");
                return;
            }

            AddContactForm acf = new AddContactForm(String.Empty);
            if (DialogResult.OK == acf.ShowDialog(this) && acf.Account != String.Empty)
            {
                messenger.ContactService.AddNewContact(acf.Account, acf.InvitationMessage);
            }
        }

        private void createCircleMenuItem_Click(object sender, EventArgs e)
        {
            //This is a demostration to tell you how to use MSNPSharp to create, block, and unblock Circle.
            messenger.ContactService.CreateCircle("test wp circle");
            messenger.ContactService.CreateCircleCompleted += new EventHandler<CircleEventArgs>(ContactService_TestingCircleAdded);
        }

        void ContactService_TestingCircleAdded(object sender, CircleEventArgs e)
        {
            //Circle created, then show you how to block.
            ////if (!e.Circle.OnBlockedList)
            ////{
            ////    messenger.ContactService.BlockCircle(e.Circle);
            ////    e.Circle.ContactBlocked += new EventHandler<EventArgs>(Circle_ContactBlocked);
            ////    Trace.WriteLine("Circle blocked: " + e.Circle.ToString());
            ////}

            ////Trace.WriteLine("Circle created: " + e.Circle.ToString());
        }

        void Circle_ContactBlocked(object sender, EventArgs e)
        {
            //Circle blocked, show you how to unblock.
            Circle circle = sender as Circle;
            if (circle != null)
            {
                messenger.ContactService.UnBlockCircle(circle);
                circle.ContactUnBlocked += new EventHandler<EventArgs>(circle_ContactUnBlocked);
                Trace.WriteLine("Circle unblocked: " + circle.ToString());
            }
        }

        void circle_ContactUnBlocked(object sender, EventArgs e)
        {
            //This demo shows you how to invite a contact to your circle.
            if (messenger.ContactList.HasContact("freezingsoft@hotmail.com", ClientType.PassportMember))
            {
                messenger.ContactService.InviteCircleMember(sender as Circle, messenger.ContactList["freezingsoft@hotmail.com", ClientType.PassportMember], "hello");
                messenger.ContactService.InviteCircleMemberCompleted += new EventHandler<CircleMemberEventArgs>(ContactService_CircleMemberInvited);
            }
        }

        void ContactService_CircleMemberInvited(object sender, CircleMemberEventArgs e)
        {
            Trace.WriteLine("Invited: " + e.Member.Hash);
        }

        private void importContactsMenuItem_Click(object sender, EventArgs e)
        {
            ImportContacts ic = new ImportContacts();
            if (ic.ShowDialog(this) == DialogResult.OK)
            {
                string invitation = ic.InvitationMessage;
                foreach (String account in ic.Contacts)
                {
                    messenger.ContactService.AddNewContact(account, invitation);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty || txtSearch.Text == "Search contacts")
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = true;
                treeViewFilterList.Visible = false;
            }
            else
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = false;
                treeViewFilterList.Visible = true;
                TreeNode foundnode = treeViewFilterList.Nodes.Add("0", "Search Results:");

                foreach (Contact contact in messenger.ContactList.All)
                {
                    if (contact.Mail.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1
                        ||
                        contact.Name.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        TreeNode newnode = foundnode.Nodes.Add(contact.Hash, contact.Name);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                    }
                }
                foundnode.Text = "Search Results: " + foundnode.Nodes.Count;
                foundnode.Expand();
            }
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Search contacts")
            {
                txtSearch.Text = String.Empty;
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty)
            {
                txtSearch.Text = "Search contacts";
            }
        }

        private void lblName_Leave(object sender, EventArgs e)
        {
            string dn = lblName.Text;
            string pm = lblPM.Text;

            List<string> lstPersonalMessage = new List<string>(new string[] { "", "" });

            if (dn != messenger.ContactList.Owner.Name)
            {

                lstPersonalMessage[0] = dn;
            }

            if (messenger.ContactList.Owner.PersonalMessage == null || pm != messenger.ContactList.Owner.PersonalMessage.Message)
            {
                lstPersonalMessage[1] = pm;

            }

            Thread updateThread = new Thread(new ParameterizedThreadStart(UpdateProfile));
            updateThread.Start(lstPersonalMessage);
        }

        private void comboStatus_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!messenger.Connected)
            {
                login_KeyPress(sender, e);
            }
        }

        private void displayImageBox_Click(object sender, EventArgs e)
        {
            if (messenger.Connected)
            {
                if (openImageDialog.ShowDialog() == DialogResult.OK)
                {
                    Image newImage = Image.FromFile(openImageDialog.FileName, true);
                    Thread updateThread = new Thread(new ParameterizedThreadStart(UpdateProfile));
                    updateThread.Start(newImage);
                }
            }
        }

        private void UpdateProfile(object profileObject)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Updating owner profile, please wait....");

            if (profileObject is Image)
            {
                messenger.Nameserver.StorageService.UpdateProfile(profileObject as Image, "MyPhoto");
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Update displayimage completed.");
            }

            if (profileObject is List<string>)
            {
                List<string> lstPersonalMessage = profileObject as List<string>;
                if (lstPersonalMessage[0] != "")
                {
                    messenger.ContactList.Owner.Name = lstPersonalMessage[0];
                }

                if (lstPersonalMessage[1] != "")
                {
                    messenger.ContactList.Owner.PersonalMessage = new PersonalMessage(lstPersonalMessage[1], MediaType.None, null, NSMessageHandler.MachineGuid);
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Update personal message completed.");
            }
        }

        private void btnSetMusic_Click(object sender, EventArgs e)
        {
            MusicForm musicForm = new MusicForm();
            if (musicForm.ShowDialog() == DialogResult.OK)
            {
                Messenger.ContactList.Owner.PersonalMessage = new PersonalMessage(
                    Messenger.ContactList.Owner.PersonalMessage.Message,
                    MediaType.Music,
                    new string[] { musicForm.Artist, musicForm.Song, musicForm.Album, "" },
                    NSMessageHandler.MachineGuid);
            }
        }

      
    }
}