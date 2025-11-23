using BinaryTestApp.Interface;
using BinaryTestApp.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTestApp.Model
{
    public enum MsgFlag : byte
    {
        FlagA = 0x01,
        FlagB = 0x02,
        FlagC = 0x03
    }

    public enum MsgSubFlag : byte
    {
        SubFlagX = 0x0A,
        SubFlagY = 0x0B,
        SubFlagZ = 0x0C
    }

    public class MsgBindingModel : INotifyPropertyChanged
    {
        public string Time { get; set; }

        public MsgFlag Flag { get; set; }

        public MsgSubModel SubModel;

        public MsgBindingModel(MsgModel model)
        {
            Time = DateTimeOffset.FromUnixTimeSeconds(model.Header.ReceiveTime).ToLocalTime().DateTime.ToString("yyyy-MM-dd HH:mm:ss");
            Flag = (MsgFlag)model.Flag;
            SubModel = model.SubModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MsgSubBindingModel : INotifyPropertyChanged
    {
        public MsgSubFlag STS;
        public MsgSubFlag SAS;
        public MsgSubFlag SCS;

        public MsgSubBindingModel(MsgSubModel subModel)
        {
            STS = (MsgSubFlag)subModel.STS;
            SAS = (MsgSubFlag)subModel.SAS;
            SCS = (MsgSubFlag)subModel.SCS;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
