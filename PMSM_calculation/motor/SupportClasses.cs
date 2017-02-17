using System;

namespace calc_from_geometryOfMotor.motor
{
    public class Constants
    {
        public static readonly double mu_0 = 4 * Math.PI * 1e-7;
    }    

    public class ParamValidationInfo
    {
        public enum MessageType
        {
            Info, Warning, Error
        }

        public MessageType msgType { get; private set; }
        public String ParamName { get; private set; }//param which invalid
        public String message { get; private set; }

        public ParamValidationInfo(String p, string msg, MessageType t)
        {
            ParamName = p;
            message = msg;
            msgType = t;
        }
    }

    public struct PointBH
    {
        public double b { get; set; }
        public double h { get; set; }
    }   
}
