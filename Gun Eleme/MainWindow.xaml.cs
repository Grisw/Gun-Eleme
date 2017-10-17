using Fiddler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private WechatUser _checkUser;
        private WechatUser checkUser
        {
            get
            {
                if (_checkUser == null)
                    _checkUser = new WechatUser();
                return _checkUser;
            }
        }

        private OauthToken unluckyUser;
        private OauthToken luckyUser;

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

        private ObservableCollection<ElemeLuckyMoney> _elemeHistoryList;
        private ObservableCollection<ElemeLuckyMoney> elemeHistoryList
        {
            get
            {
                if (_elemeHistoryList == null)
                    _elemeHistoryList = new ObservableCollection<ElemeLuckyMoney>();
                return _elemeHistoryList;
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
        
        private Thread _runThread;
        private Thread runThread {
            get
            {
                if(_runThread == null) {
                    _runThread = new Thread(() => {
                        while(_runThread != null) {
                            if (elemeQueue.Count > 0) {
                                ElemeLuckyMoney eleme;
                                lock (elemeQueue) {
                                    eleme = elemeQueue.Dequeue();
                                }
                                unluckyUser.Go(eleme, (res) => {
                                    eleme.Rest = res;
                                    if (res == 1) {
                                        luckyUser.Go(eleme, (res1) => {
                                            eleme.Rest = res1;
                                            if (res1 == 0) {
                                                gottenMoneyCount++;
                                                eleme.IsSuccess = true;
                                            }
                                            listeningMoneyCount--;
                                        }, () => {
                                            Dispatcher.Invoke(() => {
                                                elemeQueue.Enqueue(eleme);
                                                Stop();
                                                MessageBox.Show(this, "拿刀用户的OAuth Token失效！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                            });
                                        });
                                    }else if(res > 1) {
                                        lock (elemeQueue) {
                                            elemeQueue.Enqueue(eleme);
                                        }
                                        tryCount++;
                                    }else {
                                        listeningMoneyCount--;
                                    }
                                }, () => {
                                    Dispatcher.Invoke(() => {
                                        elemeQueue.Enqueue(eleme);
                                        Stop();
                                        MessageBox.Show(this, "垫刀用户的OAuth Token失效！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            wechatLogin_Button.IsEnabled = false;
            luckyUser_Button.IsEnabled = false;
            unluckyUser_Button.IsEnabled = false;
            runThread.Start();
        }

        private void Stop() {
            control_Button.Content = "开始";
            wechatLogin_Button.IsEnabled = true;
            luckyUser_Button.IsEnabled = true;
            unluckyUser_Button.IsEnabled = true;
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

            status_DataGrid.ItemsSource = elemeHistoryList;
        }

        private void wechatLogin_Button_Click(object sender, RoutedEventArgs e) {
            WechatWindow window = new WechatWindow(this, checkUser);
            if(window.ShowDialog() == true) {
                checkUserName_Label.Content = window.LoginToken.UserName;
                checkUserStatus_Label.Content = "已登录";
                checkUser.Sync(window.LoginToken, (eleme) => {
                    if (!elemeHistoryList.Contains(eleme)) {
                        lock (elemeQueue) {
                            elemeQueue.Enqueue(eleme);
                        }
                        listeningMoneyCount++;
                        Dispatcher.Invoke(() => {
                            elemeHistoryList.Insert(0, eleme);
                        });
                    }
                }, () => {
                    Dispatcher.Invoke(() => {
                        checkUserStatus_Label.Content = "登录失效";
                    });
                });
            }
        }
        
        private void control_Button_Click(object sender, RoutedEventArgs e) {
            if ((string)control_Button.Content == "开始") {
                if ((string)checkUserStatus_Label.Content == "已登录" && luckyUser != null && unluckyUser != null) {
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

        private void unluckyUser_Button_Click(object sender, RoutedEventArgs e) {
            ChooseUserWindow window = new ChooseUserWindow(this);
            if(window.ShowDialog() == true) {
                unluckyUser = window.Token;
                unluckyUserName_Label.Content = unluckyUser.UserName;
            }
        }

        private void luckyUser_Button_Click(object sender, RoutedEventArgs e) {
            ChooseUserWindow window = new ChooseUserWindow(this);
            if (window.ShowDialog() == true) {
                luckyUser = window.Token;
                luckyUserName_Label.Content = luckyUser.UserName;
            }
        }
    }
}
