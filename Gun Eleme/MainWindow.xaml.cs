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

        private bool isRunning { get; set; }
        
        private Thread runningThread
        {
            get
            {
                return new Thread((e) => {
                    ElemeLuckyMoney eleme = e as ElemeLuckyMoney;
                    while (isRunning) {
                        bool? state = null;
                        unluckyUser.Go(eleme, (res) => {
                            tryCount++;
                            eleme.Rest = res;
                            if (res == 1) {
                                luckyUser.Go(eleme, (res1) => {
                                    eleme.Rest = res1;
                                    if (res1 == 0) {
                                        gottenMoneyCount++;
                                        eleme.IsSuccess = true;
                                    }
                                    state = false;
                                }, () => {
                                    Dispatcher.Invoke(() => {
                                        state = false;
                                        Stop();
                                        MessageBox.Show(this, "拿刀用户的OAuth Token失效！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    });
                                });
                            } else if (res > 1) {
                                state = true;
                            } else {
                                state = false;
                            }
                        }, () => {
                            Dispatcher.Invoke(() => {
                                state = false;
                                Stop();
                                MessageBox.Show(this, "垫刀用户的OAuth Token失效！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        });
                        while (state == null) {
                            Thread.Sleep(100);
                        }
                        if (state == true) {
                            Thread.Sleep(3000);
                        } else {
                            listeningMoneyCount--;
                            break;
                        }
                    }
                });
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
            isRunning = true;
            new Thread(() => {
                foreach (ElemeLuckyMoney eleme in elemeHistoryList) {
                    if (eleme.Rest >= 0) {
                        runningThread.Start(eleme);
                        Thread.Sleep(1000);
                    }
                }
            }).Start();
        }

        private void Stop() {
            control_Button.Content = "开始";
            isRunning = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //LoginWindow window = new LoginWindow(this, new QqUser());
            //window.ShowDialog();
            while (!InstallCertificate()) {
                switch (MessageBox.Show(this, "请信任该证书！", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No)) {
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
            LoginWindow window = new LoginWindow(this, checkUser);
            if(window.ShowDialog() == true) {
                checkUserName_Label.Content = checkUser.UserName;
                checkUserStatus_Label.Content = "已登录";
                checkUser.Sync((eleme) => {
                    Dispatcher.Invoke(() => {
                        if (!elemeHistoryList.Contains(eleme)) {
                            listeningMoneyCount++;
                            elemeHistoryList.Insert(0, eleme);
                            runningThread.Start(eleme);
                        }
                    });
                }, () => {
                    Dispatcher.Invoke(() => {
                        checkUserStatus_Label.Content = "登录失效";
                    });
                });
            }
        }
        
        private void control_Button_Click(object sender, RoutedEventArgs e) {
            if ((string)control_Button.Content == "开始") {
                if (luckyUser != null && unluckyUser != null) {
                    Start();
                } else {
                    MessageBox.Show(this, "请选择垫刀和主刀用户的OAuth Token！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
