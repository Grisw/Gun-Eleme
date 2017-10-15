using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gun_Eleme {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {

        private WechatUser _unluckyUser;
        private WechatUser unluckyUser
        {
            get
            {
                if (_unluckyUser == null)
                    _unluckyUser = new WechatUser();
                return _unluckyUser;
            }
        }

        private Queue<ElemeLuckyMoney> _elemeQueue;
        private Queue<ElemeLuckyMoney> elemeQueue
        {
            get
            {
                if (_elemeQueue == null)
                    _elemeQueue = new Queue<ElemeLuckyMoney>();
                return _elemeQueue;
            }
        }

        private int _tryCount;
        private int tryCount {
            get
            {
                return _tryCount;
            }
            set
            {
                Dispatcher.Invoke(() => {
                    _tryCount = value;
                    tryCount_Label.Content = value;
                });
            }
        }

        private int _listeningMoneyCount;
        private int listeningMoneyCount
        {
            get
            {
                return _listeningMoneyCount;
            }
            set
            {
                Dispatcher.Invoke(() => {
                    _listeningMoneyCount = value;
                    listeningMoneyCount_Label.Content = value;
                });
            }
        }

        private int _gottenMoneyCount;
        private int gottenMoneyCount
        {
            get
            {
                return _gottenMoneyCount;
            }
            set
            {
                Dispatcher.Invoke(() => {
                    _gottenMoneyCount = value;
                    gottenMoneyCount_Label.Content = value;
                });
            }
        }

        private int _gottenMoneyAmount;
        private int gottenMoneyAmount
        {
            get
            {
                return _gottenMoneyAmount;
            }
            set
            {
                Dispatcher.Invoke(() => {
                    _gottenMoneyAmount = value;
                    gottenMoneyAmount_Label.Content = value;
                });
            }
        }

        private Thread _runThread;
        private Thread runThread {
            get
            {
                if(_runThread == null) {
                    _runThread = new Thread(() => {
                        OauthToken luckyToken = OauthToken.Load("lucky");
                        OauthToken unluckyToken = OauthToken.Load("unlucky");

                        while(_runThread != null) {
                            if (elemeQueue.Count > 0) {
                                ElemeLuckyMoney eleme = elemeQueue.Dequeue();
                                unluckyToken.Go(eleme, (res) => {
                                    Console.WriteLine(res + ":" + eleme.Url);
                                    if(res == 1) {
                                        luckyToken.Go(eleme, (res1) => {
                                            if (res1 == 0) {
                                                gottenMoneyCount++;
                                                gottenMoneyAmount += eleme.LuckyNum;
                                            }
                                            listeningMoneyCount--;
                                        }, () => {
                                            Dispatcher.Invoke(() => {
                                                luckyOauthStatus_Label.Content = "Token失效";
                                                elemeQueue.Enqueue(eleme);
                                                Stop();
                                            });
                                        });
                                    }else if(res > 1) {
                                        elemeQueue.Enqueue(eleme);
                                        tryCount++;
                                    }else {
                                        listeningMoneyCount--;
                                    }
                                }, () => {
                                    Dispatcher.Invoke(() => {
                                        unluckyUserStatus_Label.Content = ((string)unluckyUserStatus_Label.Content).Split('；')[0] + "；Token失效";
                                        elemeQueue.Enqueue(eleme);
                                        Stop();
                                    });
                                });
                            }else {
                                Thread.Sleep(100);
                            }
                        }
                    });
                }
                return _runThread;
            }
            set
            {
                _runThread = value;
            }
        }

        public MainWindow() {
            InitializeComponent();
        }

        private bool InstallCertificate() {
            if (!CertMaker.rootCertExists()) {
                CertMaker.removeFiddlerGeneratedCerts(true);
                if (!CertMaker.createRootCert())
                    return false;

                if (!CertMaker.trustRootCert())
                    return false;
            }

            return true;
        }

        private void Start() {
            control_Button.Content = "停止";
            unluckyWechatLogin_Button.IsEnabled = false;
            unluckyWechatOauth_Button.IsEnabled = false;
            luckyWechatOauth_Button.IsEnabled = false;
            runThread.Start();
        }

        private void Stop() {
            control_Button.Content = "开始";
            unluckyWechatLogin_Button.IsEnabled = true;
            unluckyWechatOauth_Button.IsEnabled = true;
            luckyWechatOauth_Button.IsEnabled = true;
            runThread = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            while (!InstallCertificate()) {
                switch(MessageBox.Show(this, "请信任该证书！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)) {
                    case MessageBoxResult.Yes:
                        break;
                    default:
                        Close();
                        return;
                }
            }
            FiddlerApplication.SetAppDisplayName("Gun Eleme");

            OauthToken token = OauthToken.Load("lucky");
            if(token != null) {
                luckyUserName_Label.Content = token.UserName;
                luckyOauthStatus_Label.Content = "已获取Token";
            }
            token = OauthToken.Load("unlucky");
            if (token != null) {
                unluckyUserName_Label.Content = token.UserName;
                unluckyUserStatus_Label.Content = ((string)unluckyUserStatus_Label.Content).Split('；')[0] + "；已获取Token";
            }
        }

        private void unluckyWechatLogin_Button_Click(object sender, RoutedEventArgs e) {
            WechatWindow window = new WechatWindow(this, unluckyUser);
            if(window.ShowDialog() == true) {
                unluckyUserName_Label.Content = window.LoginToken.UserName;
                unluckyUserStatus_Label.Content = "已登录；" + ((string)unluckyUserStatus_Label.Content).Split('；')[1];
                unluckyUser.Sync(window.LoginToken, (eleme) => {
                    if (!elemeQueue.Contains(eleme)) {
                        elemeQueue.Enqueue(eleme);
                        listeningMoneyCount++;
                    }
                }, () => {
                    Dispatcher.Invoke(() => {
                        unluckyUserStatus_Label.Content = "登录失效；" + ((string)unluckyUserStatus_Label.Content).Split('；')[1];
                        Stop();
                    });
                });
            }
        }

        private void unluckyWechatOauth_Button_Click(object sender, RoutedEventArgs e) {
            OauthWindow window = new OauthWindow(this);
            if (window.ShowDialog() == true) {
                unluckyUserStatus_Label.Content = ((string)unluckyUserStatus_Label.Content).Split('；')[0] + "；已获取Token";
                window.oauthToken.Save("unlucky");
            }
        }

        private void luckyWechatOauth_Button_Click(object sender, RoutedEventArgs e) {
            OauthWindow window = new OauthWindow(this);
            if (window.ShowDialog() == true) {
                luckyUserName_Label.Content = window.oauthToken.UserName;
                luckyOauthStatus_Label.Content = "已获取Token";
                window.oauthToken.Save("lucky");
            }
        }

        private void control_Button_Click(object sender, RoutedEventArgs e) {
            if ((string)control_Button.Content == "开始") {
                if((string)luckyOauthStatus_Label.Content == "已获取Token" && (string)unluckyUserStatus_Label.Content == "已登录；已获取Token") {
                    Start();
                } else {
                    MessageBox.Show(this, "请先完成登录和Token的获取（建议先获取Token）！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }else {
                Stop();
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            Environment.Exit(0);
        }
    }
}
