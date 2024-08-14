using GalaSoft.MvvmLight.Messaging;
using System.Windows;
using FR_Validate_Demo.Model;
using FR_Validate_Demo.ViewModel;

namespace FR_Validate_Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            Messenger.Default.Register<NotificationMessage>(this, Msg.VM, ShowConfirmMsg);
            Messenger.Default.Register<string>(this, Msg.VM, ShowMsg);

            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void ShowMsg(string strMsg)
        {
            MessageBoxImage mboxImg;

            if (strMsg.ToUpper().Contains(StaticVar.ERROR))
                mboxImg = MessageBoxImage.Error;
            else if (strMsg.ToUpper().Contains(StaticVar.WARNING))
                mboxImg = MessageBoxImage.Warning;
            else
                mboxImg = MessageBoxImage.Information;

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, strMsg, StaticVar.TITLE, MessageBoxButton.OK, mboxImg);
            });
        }

        private void ShowConfirmMsg(NotificationMessage noti)
        {
            string strSwitch = noti.Target == null ? noti.Notification : noti.Target.ToString();
            string strMsg = noti.Target != null ? noti.Notification : "Missing message!";
            MessageBoxResult mbR;

            mbR = MessageBox.Show(this, strMsg, StaticVar.TITLE, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (mbR == MessageBoxResult.Yes)
                Messenger.Default.Send(new NotificationMessage(this, strSwitch, StaticVar.OK), Msg.VIEW);
            else
                Messenger.Default.Send(new NotificationMessage(this, strSwitch, StaticVar.CANCEL), Msg.VIEW);
        }
    }
}