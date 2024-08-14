using GalaSoft.MvvmLight;
using FR_Core_Tech_Demo.Model;
using System;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using System.IO.Ports;
using GalaSoft.MvvmLight.Command;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FaceAuthNS;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Windows.Threading;
using System.Text;

namespace FR_Core_Tech_Demo.ViewModel
{
   
    public class MainViewModel : ViewModelBase
    {
        #region For FR

        private VideoCapture _videoCapture;

        // To control Camera frame.
        DispatcherTimer timerCameraFrame = new DispatcherTimer();

        short _sqrcCellUnit = 150;

        // For each frame capture, we will convert it to Bitmap. 
        // We will use this bitmap when generating Face SQRC.
        Bitmap _capture;


        private bool frImage;
        public bool FrImage
        {
            get { return frImage; }
            set { Set(ref frImage , value); }
        }


        private BitmapImage registeredFace;
        public BitmapImage RegisteredFace
        {
            get { return registeredFace; }
            set { Set(ref registeredFace , value); }
        }


        #endregion


        #region >>>>>>>>> IMAGE SOURCE

        private BitmapImage _registerImage;
        public BitmapImage RegisterImage
        {
            get { return _registerImage; }
            set { Set(ref _registerImage, value); }
        }


        private BitmapImage _registerVideo;
        public BitmapImage RegisterVideo
        {
            get { return _registerVideo; }
            set { Set(ref _registerVideo, value); }
        }


        #endregion



        private static Logger logger = LogManager.GetCurrentClassLogger();
      


        #region Private Constant

        private const string tag_Clear = "CLEAR";
        private const string tag_Generate = "GENERTE";
       
        private const string tag_Test = "TEST";
        private const string tag_Camera_Refresh = "CAMERA_REFRESH";
        private const string tag_Camera_Shot = "CAMERA_SHOT";     

        #endregion


        #region TAG Binding

        public string TAG_GENERATE
        {
            get { return tag_Generate; }
        }


        public string TAG_CAMERA_SHOT
        {
            get { return tag_Camera_Shot; }
        }

        public string TAG_CAMERA_REFRESH
        {
            get { return tag_Camera_Refresh; }
        }

        public string TAG_CLEAR
        {
            get { return tag_Clear; }
        }

        public string TAG_TEST
        {
            get { return tag_Test; }
        }

        #endregion


        #region Binding

        private string companyName;
        public string CompanyName
        {
            get { return companyName; }
            set { Set(ref companyName, value); }
        }


        private string address;
        public string Address
        {
            get { return address; }
            set { Set(ref address, value); }
        }


        private string contactNo;
        public string ContactNo
        {
            get { return contactNo; }
            set { Set(ref contactNo, value); }
        }


        private string name;
        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        

        private string permitNo;
        public string PermitNo
        {
            get { return permitNo; }
            set { Set(ref permitNo , value); }
        }

        

        private string templatePerson;
        public string TemplatePerson
        {
            get { return templatePerson; }
            set { Set(ref templatePerson , value); }
        }


        private bool activateFR;
        public bool ActivateFR
        {
            get { return activateFR; }
            set { Set(ref activateFR , value); }
        }


        private bool okFaceSqrc;
        public bool OkFaceSqrc
        {
            get { return okFaceSqrc; }
            set {
                Set(ref okFaceSqrc , value);
                if (value)
                {
                    ActivateFR = true;
                   

                }
            }
        }


        private string titleVersion;
        public string TitleVersion
        {
            get { return titleVersion; }
            set { Set(ref titleVersion , value); }
        }


        private string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title , value); }
        }


        private string version;
        public string Version
        {
            get { return version; }
            set { Set(ref version , value); }
        }


        private string issueDate;
        public string IssueDate
        {
            get { return issueDate; }
            set { Set(ref issueDate, value); }
        }


        private DateTime expirySetDate;
        public DateTime ExpirySetDate
        {
            get { return expirySetDate; }
            set { Set(ref expirySetDate , value); }
        }


        private DateTime expiryStartDate;
        public DateTime ExpiryStartDate
        {
            get { return expiryStartDate; }
            set { Set(ref expiryStartDate , value); }
        }


        private DateTime expiryEndDate;

        public DateTime ExpiryEndDate
        {
            get { return expiryEndDate; }
            set { Set(ref expiryEndDate , value); }
        }


        private DateTime selectedExpiryDate;
        public DateTime SelectedExpiryDate
        {
            get { return selectedExpiryDate; }
            set { Set(ref selectedExpiryDate , value); }
        }


        #endregion


        #region ICommand

        public ICommand CmdJob { get; private set; }
        public ICommand CmdCamera { get; private set; }
        public ICommand CmdClear { get; private set; }
  

        #endregion


        private readonly IDataService _dataService;



        public MainViewModel(IDataService dataService)
        {
           
            CmdJob = new RelayCommand<object>(ActionJob);
            CmdCamera = new RelayCommand<object>(ActionCamera);
            CmdClear = new RelayCommand<object>(ActionClear);
         
            logger.Info("Demo is started.");


            if (!Directory.Exists(Properties.Settings.Default.IMG_PATH))
            {
                Directory.CreateDirectory(Properties.Settings.Default.IMG_PATH);
            }

            if (InitFR())
            {
                // Set/Define Timer.
                timerCameraFrame.Interval = new TimeSpan(0, 0, 0, 0, 40);
                timerCameraFrame.Tick += timerCameraFrame_Tick;
            }

            InitScreen();
        }

        private void timerCameraFrame_Tick(object sender, EventArgs e)
        {
            try
            {
                using (var frame = new Mat())
                {
                    _videoCapture.Read(frame);
                    if (frame.Cols > 0)
                    {
                        Bitmap bitmap = BitmapConverter.ToBitmap(frame);
                        // Convert each frame to Bitmap.
                        _capture = bitmap;
                        // This is to display on screen only.
                        RegisterVideo = Mat2BitmapImage(frame);
                    }
                }
            }
            catch (Exception ex)
            {
                timerCameraFrame.Stop();
                logger.Error("Mytimer exception: " + ex.Message);
            }
        }

        private bool InitFR()
        {
            try
            {
                var errMsg = "";

                // Initialize FaceAuthentication.
                long result = FaceAuthOpt.InitializeFA(FaceAuthOpt.LIC_SQRC_GEN);
                switch (result)
                {
                    case 0: errMsg = ""; break;
                    case 100: errMsg = "Face SQRC DLL error: Fail to read Face.conf file."; break;
                    case 101: errMsg = "Face SQRC DLL error: Invalid settings."; break;
                    case 102: errMsg = "Face SQRC DLL error: COM port open failed."; break;
                    case 103: errMsg = "Face SQRC DLL error: Incorrect environment (Key file does not exist)."; break;
                    default: errMsg = "Face SQRC DLL error: Internal FR error!"; break;
                }

                if (!string.IsNullOrEmpty(errMsg))
                {
                    Reply rp = new Reply
                    {
                        IsOk = false,
                        Msg = "FR Initialization failed. Make sure Face.config file is correctly set and FR license is correctly registered" + Environment.NewLine +
                        "Err: " + errMsg,
                        ErrMsg = "Err Code: " + ErrCode.Err_FRInitialization + ". Err: " + errMsg
                    };
                    logger.Error("FR initialization failed. Err: " + errMsg);
                    ForwardErrMsg(rp);

                    return false;
                }
                else
                {
                    return true;
                }

            }
            catch (Exception e)
            {
                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "FR Initialization failed." + Environment.NewLine + "Err: " + e.Message,
                    ErrMsg = "Err Code: " + ErrCode.Err_FRInitialization + ". Err: " + e.Message
                };
                logger.Error("FR initialization failed. Err: " + e.Message);
                ForwardErrMsg(rp);

                return false;
            }
        }


        private void ForwardErrMsg(Reply reply)
        {
            if (!string.IsNullOrEmpty(reply.ErrMsg))
            {
                if (Properties.Settings.Default.DEBUG_FLAG)      // If Flag is true, show Exception Error. Log will be logged regardless of this flag. This flag is only for user to see.
                    Messenger.Default.Send(StaticVar.ERROR + Environment.NewLine + Environment.NewLine + reply.Msg + Environment.NewLine + Environment.NewLine + reply.ErrMsg, Msg.VM);
                else
                    Messenger.Default.Send(StaticVar.ERROR + Environment.NewLine + Environment.NewLine + reply.Msg, Msg.VM);
            }
            else if (reply.IsOk == false)
                Messenger.Default.Send(StaticVar.ERROR + Environment.NewLine + Environment.NewLine + reply.Msg, Msg.VM);
            else if (reply.IsOk == true && !string.IsNullOrEmpty(reply.WarningMsg))
                Messenger.Default.Send(StaticVar.WARNING + Environment.NewLine + Environment.NewLine + reply.WarningMsg, Msg.VM);
            else
                Messenger.Default.Send(reply.Msg, Msg.VM);
        }


        private void InitScreen()
        {
            Title = Properties.Settings.Default.TITLE;
            Version = "Ver: " + MyApp.GetVersion();

            TitleVersion = Title + "   " + " { " + version + " } ";

            CompanyName = "DENSO INTERNATIONAL ASIA PTE. LTD.";
            Address = "51 Science Park Road, #01-10, The Aries, Singapore.";
            ContactNo = "67768268";
            Name = "Denso";

            PermitNo = "DNQR0123";

            IssueDate = DateTime.Now.ToString("yyyy-MMM-dd");
            ExpirySetDate = DateTime.Now.AddMonths(3);
            ExpiryStartDate = DateTime.Now;
            ExpiryEndDate = DateTime.Now.AddYears(1);
            SelectedExpiryDate = DateTime.Now.AddMonths(3);

            ActivateFR = true;

            // Start Camera to take photo.
            FrImage = false;
            StartCamera();
            
        }


        #region >>>>>>>>> OpenCV related

        private Bitmap Mat2Bitmap(Mat image)
        {
            return BitmapConverter.ToBitmap(image);
        }

        private BitmapImage Mat2BitmapImage(Mat image)
        {
            Bitmap bitmap = Mat2Bitmap(image);
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

        private System.Drawing.Bitmap BitmapImageToBitmap(BitmapImage bitImg)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitImg));
                enc.Save(outStream);
                Bitmap bmp = new Bitmap(outStream);
                return new Bitmap(bmp);
            }
        }

        public BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        #endregion

        private void GenerateCode()
        {
            var publicData = name;
            var privateData = name.Replace(",", "") + "," + contactNo.Replace(",", "") + "," + permitNo.Replace(",", "") + "," + selectedExpiryDate.ToString("yyyy-MMM-dd");

            logger.Info("Face SQRC generating started.");
            Reply r = GenerateFrSqrc(publicData, privateData);

            if (r.IsOk == false)
            {
                ForwardErrMsg(r);
            }
            else
            {
                logger.Info("Face Recognition SQRC generating ended.");
                ForwardErrMsg(new Reply() { IsOk = true, Msg = "Face Recognition SQRC was generated successfully!" });
            }
        }

        private Reply GenerateFrSqrc(string openData, string closeData)
        {
            Reply rp = new Reply();

            try
            {
                string errMsg = "";

                if (_registerVideo != null)
                {
                    // Set FR SQRC Expiry Date.
                    DateTime dt = DateTime.Now;
                    dt = dt.AddDays(Properties.Settings.Default.FR_SQRC_EXPIRY_DAYS);
                    var faceOpt = new FaceAuthOpt();
                    faceOpt.SetExpirationDate(dt);

                    // Define file name and output diretory.
                    var fileName = Path.Combine(Properties.Settings.Default.IMG_PATH, "FR_" + DateTime.Now.ToString("hhmmss") + ".png");

                    // Define Private, Public data for SQRC.
                    byte[] userData = Encoding.GetEncoding("Shift_JIS").GetBytes(closeData);
                    byte[] publicData = Encoding.GetEncoding("Shift_JIS").GetBytes(openData);

                    _sqrcCellUnit = 203;
                    // Generate Face SQRC.
                    int result = FaceAuthOpt.SaveSQRC(_capture, userData, publicData, Properties.Settings.Default.SQRC_KEY,
                        fileName, FaceAuthOpt.FORMAT_PNG, _sqrcCellUnit);

                    errMsg = "";

                    switch (result)
                    {
                        case 0:
                            errMsg = "";
                            break;
                        case 1:
                            errMsg = "Incorrect Image Save Location.";
                            break;
                        case 2:
                            errMsg = "Face detection failed.";
                            break;
                        case 3:
                            errMsg = "Library load error.";
                            InitFR();
                            break;
                        case 4:
                            errMsg = "QR data encryption failed.";
                            break;
                        case 5:
                            errMsg = "Incorrect setting of cell unit.";
                            break;
                        case 6:
                            errMsg = "Generating face feature data failed.";
                            break;
                        case 7:
                            errMsg = "Incorrect user data.";
                            break;
                        case 8:
                            errMsg = "Incorrect open data.";
                            break;
                        case 9:
                            errMsg = "Failed to save face recognition QR Code.";
                            break;
                        default:
                            errMsg = "System Error (InitializeFA) [return" + result + "]";
                            break;
                    }

                    if (result == 0)
                    {
                        rp.ImgPath = fileName;
                        rp.IsOk = true;
                        rp.Msg = "";
                        logger.Debug("FR generated successfully.");
                    }
                    else
                    {
                        rp.IsOk = false;
                        rp.Msg = errMsg;

                        logger.Debug("FR Generation failed. Err: " + errMsg);
                    }
                }
            }
            catch (Exception e)
            {
                rp.IsOk = false;
                rp.Msg = "Generating Face Recognition SQRC failed. Err: " + e.Message;
                rp.ErrMsg = "Err Code: " + ErrCode.Err_GenerateFrSqrc + ". Err: " + e.Message;
                logger.Error("FR SQRC g eneration failed.");
            }

            return rp;
        }

      

        #region ACTION

        private void ActionClear(object obj)
        {
            string strTag = obj == null ? "" : obj.ToString().Trim();
            if (string.IsNullOrEmpty(strTag)) return;

            switch (strTag)
            {
                case tag_Clear:
                    InitScreen();
                    break;
            }
        }

        private void ActionJob(object obj)
        {
            string strTag = obj == null ? "" : obj.ToString().Trim();
            if (string.IsNullOrEmpty(strTag)) return;

            switch (strTag)
            {
                case tag_Generate:
                    GenerateCode();
                    break;
            }
        }

        private void ActionCamera(object obj)
        {
            string strTag = obj == null ? "" : obj.ToString().Trim();
            if (string.IsNullOrEmpty(strTag)) return;

            switch (strTag)
            {
                case tag_Camera_Shot: TakePhoto(); break;
                case tag_Camera_Refresh: RefreshCamera(); break;
               
            }
        }

        #endregion


        private void TakePhoto()
        {
            try
            {
                // Stop timer, we will use whatever photo for current (last) frame.
                timerCameraFrame.Stop();
            }
            catch (Exception e)
            {
                logger.Error("Job_TakePhoto stop timer failed. Details: " + e.Message);
            }

            _videoCapture.Release();
        
        }

        // Start Video Frame.
        private void StartCamera()
        {
            try
            {
                FrImage = false;

                if (_videoCapture != null)
                    _videoCapture.Release();

                _videoCapture = new VideoCapture(Properties.Settings.Default.CAMERA_NO);
                if (_videoCapture.IsOpened())
                {
                    timerCameraFrame.Start();

                    logger.Info("Camera is started.");
                }
                else
                {
                    Reply rp = new Reply
                    {
                        IsOk = false,
                        Msg = "Open Camera Failed!" + Environment.NewLine + "Camera is in used by other application!",
                        ErrMsg = "Err Code: " + ErrCode.Err_StartCamera + ". Err: Camera is in used by other application!"
                    };

                    ForwardErrMsg(rp);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Open camera failed. Details: " + ex.Message);

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "Open Camera Failed!" + Environment.NewLine +
                         "Err: " + ex.Message,
                    ErrMsg = "Err Code: " + ErrCode.Err_StartCamera + ". Err: " + ex.Message
                };
                ForwardErrMsg(rp);
            }
        }

        private void RefreshCamera()
        {
            FrImage = false;
            StartCamera();
        }


        public override void Cleanup()
        {
            // Clean up if needed
        
            base.Cleanup();
        }

       
    }
}