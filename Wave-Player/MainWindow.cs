using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wave_Player
{
    partial class MainWindow
    {
        public bool IsShuffleEnabled
        {
            get { return _isShuffleEnabled; }
            set { _isShuffleEnabled = value; }
        }

        public bool IsRepeatEnabled
        {
            get { return _isRepeatEnabled; }
            set { _isRepeatEnabled = value; }
        }
    }
}
