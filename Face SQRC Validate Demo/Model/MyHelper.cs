using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;

namespace FR_Validate_Demo.Model
{
    internal enum ErrCode
    {
        Err_Null = 0,
        Err_FRInitialization = 101,
        Err_StartMainJob = 102,
        Err_StartCamera = 103,
        Err_ReadFaceSQRC = 104,
        Err_Verifyface = 105,
        Err_StopAllFrRelated = 109,
        Err_Cleanup = 110,
        Err_SqrcContents = 111
    }

    internal enum Msg
    {
        VM,
        VIEW,
        MODEL
    }

    internal class StaticVar
    {
        internal const string ERROR = "[ ERROR ]";
        internal const string WARNING = "[ WARNING ]";
        internal const string INFO = "[ INFO ]";

        internal const string TITLE = "DENSO QR CORE TECH DEMO";
        internal const string OK = "OK";
        internal const string CANCEL = "CANCEL";
    }

    internal static class MyApp
    {
        internal static string GetAppPath()
        {
            var path = Path.GetDirectoryName(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
            return path;
        }

        internal static string GetVersion()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
            return asm.GetName().Version.Major.ToString() + "." + asm.GetName().Version.Minor.ToString() + "." + asm.GetName().Version.Revision.ToString();
        }

        internal static string GetDevDate()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileInfo fileInfo = new FileInfo(assembly.Location);
            return fileInfo.LastWriteTime.ToString("yyyy MMM dd");
        }

        internal static string GetExeName()
        {
            return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        }
    }

    public class Reply
    {
        public string ImgPath { get; set; }
        public bool? IsOk { get; set; }

        public string Msg { get; set; }
        public string WarningMsg { get; set; }

        public string ErrMsg { get; set; }

        public object CustomObj { get; set; }

        public Reply()
        {
            IsOk = false;
            Msg = "";
            WarningMsg = "";
            ErrMsg = "";
            ImgPath = "";
        }

        public Reply(bool? b, string msg, string wMsg = "", string errMsg = "", string imgPath = "")
        {
            IsOk = b;
            Msg = msg;
            WarningMsg = wMsg;
            ErrMsg = errMsg;
            ImgPath = imgPath;
        }
    }

    internal class MyConverter
    {
        internal bool ToBool(object obj)
        {
            if (obj is DBNull || obj == null)
                return false;

            bool bOut = false;
            if (bool.TryParse(obj.ToString().Trim(), out bOut))
                return bOut;
            else
                return false;
        }

        internal int ToInt32(object obj)
        {
            if (obj is DBNull || obj == null)
                return 0;

            int iOut;
            if (int.TryParse(obj.ToString().Trim(), out iOut))
                return iOut;
            else
                return 0;
        }

        decimal ToDecimal(object obj)
        {
            if (obj is DBNull || obj == null)
                return 0.0M;

            decimal dOut;
            if (decimal.TryParse(obj.ToString().Trim(), out dOut))
                return dOut;
            else
                return 0.0M;
        }

        internal byte[] StringToByteAry(object obj)
        {
            if (obj is DBNull || obj == null)
                return new byte[0];
            else
                return Encoding.ASCII.GetBytes(obj.ToString().Trim());
        }

        internal string ByteToString(object obj)
        {
            if (obj is DBNull || obj == null)
                return "";
            else
            {
                if (obj is byte[])
                    return Encoding.ASCII.GetString((obj as byte[]));
                else
                    return "";
            }
        }

        internal string ToString(object obj)
        {
            if (obj is DBNull || obj == null)
                return "";
            else
                return obj.ToString().Trim();
        }

        int Date2Int(object obj, bool b4Year = false)
        {
            try
            {
                if (obj is DBNull || obj == null)
                    return 0;
                else if (string.IsNullOrEmpty(obj.ToString().Trim()))
                    return 0;

                DateTime dt = (DateTime)obj;

                string strDate = "";
                if (b4Year)
                    strDate = dt.ToString("yyyyMMdd");
                else
                    strDate = dt.ToString("yyMMdd");

                int iOut;
                if (int.TryParse(strDate, out iOut))
                    return iOut;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        int ToIntDate(object obj)
        {
            if (obj is DBNull || obj == null)
                return 0;

            string inp = obj.ToString().Trim();

            // If it is already in 8 digit format (or) only one Zero, use normal int converter instead of current one.
            if (inp.Length == 8 || inp.Length == 1)
                return ToInt32(inp);

            inp = inp.PadLeft(6, '0');

            DateTime dt = DateTime.ParseExact(inp, "yyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            string outp = dt.ToString("yyyyMMdd");

            int iOut;
            if (int.TryParse(outp, out iOut))
                return iOut;
            else
                return 0;
        }

        /// <summary>
        /// Exception will be thrown if incorrect Date in String format.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal DateTime ToDate(object obj)
        {

            if (obj is DBNull || obj == null)
                throw new InvalidCastException();

            string inp = obj.ToString().Trim();

            try
            {
                return DateTime.ParseExact(inp, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw;
            }
        }


        internal DateTime? ToNullDate(object obj)
        {
            if (obj is DBNull || obj == null)
                return null;

            string inp = obj.ToString().Trim();

            try
            {
                return DateTime.ParseExact(inp, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
    }

}
