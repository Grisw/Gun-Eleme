using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gun_Eleme {
    public class ElemeLuckyMoney {
        public string Url { get; set; }
        public string Sn { get; set; }
        public int LuckyNum { get; set; }
        public decimal Amount { get; set; }
        public int Rest { get; set; }
        public bool IsSuccess { get; set; }

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
