using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gun_Eleme {
    public class ElemeLuckyMoney : INotifyPropertyChanged {
        public string Url { get; set; }
        public string Sn { get; set; }
        public int LuckyNum { get; set; }

        private decimal _amount;
        public decimal Amount
        {
            get
            {
                return _amount;
            }
            set
            {
                if(_amount != value) {
                    _amount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Amount"));
                }
            }
        }

        private int _rest;
        public int Rest
        {
            get
            {
                return _rest;
            }
            set
            {
                if(_rest != value) {
                    _rest = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Rest"));
                }
            }
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get
            {
                return _isSuccess;
            }
            set
            {
                if(_isSuccess != value) {
                    _isSuccess = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsSuccess"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override bool Equals(object obj) {
            if(obj is ElemeLuckyMoney) {
                return ((ElemeLuckyMoney)obj).Sn == Sn;
            }
            return false;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}
