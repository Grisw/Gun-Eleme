﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gun_Eleme {
    /// <summary>
    /// WechatWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WechatWindow : Window {

        public LoginToken LoginToken { get; private set; }

        private WechatUser user { get; set; }

        public WechatWindow(Window owner, WechatUser user) {
            InitializeComponent();

            Owner = owner;
            this.user = user;
        }

        private void updateQrcode() {
            user.GetQrcode((isSuccess, uuid) => {
                Dispatcher.Invoke(() => {
                    if (isSuccess) {
                        qrcode_Image.Stretch = Stretch.Uniform;
                        qrcode_Image.Source = new BitmapImage(new Uri("https://login.weixin.qq.com/qrcode/" + uuid));
                        Action<bool, LoginToken> onCheckLoginCompleted = null;
                        onCheckLoginCompleted = (isSuccess_1, loginToken) => {
                            Dispatcher.Invoke(() => {
                                if (isSuccess_1) {
                                    DialogResult = true;
                                    LoginToken = loginToken;
                                    Close();
                                } else {
                                    MessageBox.Show(this, "登录失败！请重新登录！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    updateQrcode();
                                }
                            });
                        };
                        user.CheckLogin(uuid, 1, onCheckLoginCompleted);
                    } else {
                        qrcode_Image.Stretch = Stretch.None;
                        qrcode_Image.Source = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.warning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        updateQrcode();
                    }
                });
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            updateQrcode();
        }
    }
}
