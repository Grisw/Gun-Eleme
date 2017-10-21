using Microsoft.JScript.Vsa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gun_Eleme {
    public abstract class User {
        
        protected static VsaEngine _vsaEngine;
        protected static VsaEngine vsaEngine
        {
            get
            {
                if (_vsaEngine == null)
                    _vsaEngine = VsaEngine.CreateEngine();
                return _vsaEngine;
            }
        }
        
        public abstract void GetQrcode(Action<bool, string> onCompleted);
        public abstract void CheckLogin(Action<bool> onCompleted);
        public abstract void Sync(Action<ElemeLuckyMoney> onReceived, Action onExpired);
        public abstract string GetDisplayName();
    }
}
