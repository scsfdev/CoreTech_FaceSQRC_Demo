using FaceAuthNS;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FR_Validate_Demo.Model;

namespace FR_Validate_Demo.ViewModel
{
   
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IDataService _dataService;
       
        private static Logger logger = LogManager.GetCurrentClassLogger();

      
        
        #region Binding

     
        private bool _visibleSqrcGrid;
        public bool VisibleSqrcGrid
        {
            get { return _visibleSqrcGrid; }
            set { Set(ref _visibleSqrcGrid, value); }
        }


       
        private string _scanInput;

        public string ScanInput
        {
            get { return _scanInput; }
            set {
                Set(ref _scanInput , value);
                
            }
        }
        

        private string titleVersion;
        public string TitleVersion
        {
            get { return titleVersion; }
            set { Set(ref titleVersion, value); }
        }


        private string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }


        private string version;
        public string Version
        {
            get { return version; }
            set { Set(ref version, value); }
        }


        private string _hiddenData;
        public string HiddenData
        {
            get { return _hiddenData; }
            set { Set(ref _hiddenData , value); }
        }


        private string _useScanner;
        public string UseScanner
        {
            get { return _useScanner; }
            set { Set(ref _useScanner , value); }
        }


        #endregion

       

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

       


        #region For FR

        private VideoCapture _videoCapture;

        // To control Camera frame.
        DispatcherTimer timerCameraFrame = new DispatcherTimer();


        // To monitor SQRC is successfully read or no reading.
        // Once it is a successful read, start verification timer (timerCameraFrame).
        DispatcherTimer timerQr = new DispatcherTimer();


        // To reset screen back for next Face verification.
        DispatcherTimer timerReset = new DispatcherTimer();

        // Thread to read FaceSQRC code.
        private Thread _thread;

        // To control thread running state.
        private bool _isThreadRunning = false;

        // Once SQRC is read successfully, this flag will turn on True.
        private bool _isQrRead = false;

        // Store SQRC Private data info.
        string userData = "";

        // Check Count will increase every time we do VerifyFace.
        int _checkCount = 0;

        // Success Count will increase for each VerifyFace pass.
        int _successCount = 0;

        // If successfully verify face (for each frame), we will turn on this variable.
        // By using this variable, we can show OK or NG icon on screen.
        bool _isAuthenticated = false;

        string errMsg = "";



        // For each frame capture, we will convert it to Bitmap. 
        // We will use this bitmap when generating Face SQRC.
        Bitmap _capture;

        


        private BitmapImage registeredFace;
        public BitmapImage RegisteredFace
        {
            get { return registeredFace; }
            set { Set(ref registeredFace, value); }
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

        private Bitmap DrawRectangle(int[] recFace, Bitmap bitmap)
        {
            Pen p;
            if (_isAuthenticated)
                p = new Pen(Color.GreenYellow, 5);
            else
                p = new Pen(Color.Red, 5);

            Graphics g = Graphics.FromImage(bitmap);
            g.DrawRectangle(p, recFace[0], recFace[1], recFace[2] - recFace[0], recFace[3] - recFace[1]);

            p.Dispose();
            g.Dispose();

            return bitmap;
        }

        #endregion


        #region General Binding

        private string _scanResult;
        public string ScanResult
        {
            get { return _scanResult; }
            set { Set(ref _scanResult, value); }
        }

        private string _scannerImage;
        public string ScannerImage
        {
            get { return _scannerImage; }
            set { Set(ref _scannerImage, value); }
        }

        private bool _visibleScannerImage;
        public bool VisibleScannerImage
        {
            get { return _visibleScannerImage; }
            set { Set(ref _visibleScannerImage, value); }
        }


        private bool _visibleFrHiddenData;
        public bool VisibleFrHiddenData
        {
            get { return _visibleFrHiddenData; }
            set { Set(ref _visibleFrHiddenData , value); }
        }



        #endregion


        #region >>>>>>>>> VERIFY Binding

        private BitmapImage _faceVerifyImage;
        public BitmapImage FaceVerifyImage
        {
            get { return _faceVerifyImage; }
            set { Set(ref _faceVerifyImage, value); }
        }


        private BitmapImage _faceVerifyFrame;
        public BitmapImage FaceVerifyFrame
        {
            get { return _faceVerifyFrame; }
            set { Set(ref _faceVerifyFrame, value); }
        }

        
        private bool _visibleCamera;
        public bool VisibleCamera
        {
            get { return _visibleCamera; }
            set { Set(ref _visibleCamera, value); }
        }


        private bool _visibleStill;
        public bool VisibleStill
        {
            get { return _visibleStill; }
            set { Set(ref _visibleStill, value); }
        }



        private bool _scanResultOk;
        public bool ScanResultOk
        {
            get { return _scanResultOk; }
            set { Set(ref _scanResultOk, value); }
        }

        private bool _scanResultNg;
        public bool ScanResultNg
        {
            get { return _scanResultNg; }
            set { Set(ref _scanResultNg, value); }
        }


        #endregion


        private void InitGuiScanner()
        {
            ScanResultOk = false;
            ScanResultNg = false;
            VisibleCamera = false;
            VisibleStill = false;
            VisibleScannerImage = true;
            HiddenData = "";
            ScanInput = "";
            VisibleFrHiddenData = false;
            ScanResult = ">> Scan your Face SQRC <<";
        }

       

        private bool InitFR()
        {
            try
            {
                var errMsg = "";

                // Initialize FaceAuthentication.
                long result = FaceAuthOpt.InitializeFA(FaceAuthOpt.LIC_SQRC_AUTH | FaceAuthOpt.LIC_QR_AUTH);
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
                    logger.Error("Err Code: " + ErrCode.Err_FRInitialization + ". FR initialization failed. Err: " + errMsg);

                    Reply rp = new Reply
                    {
                        IsOk = false,
                        Msg = "FR Initialization failed. Make sure Face.config file is correctly set and FR license is correctly registered" + Environment.NewLine +
                        "Err: " + errMsg,
                        ErrMsg = "Err Code: " + ErrCode.Err_FRInitialization + ". Err: " + errMsg
                    };
                    
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
                logger.Error("Err Code: " + ErrCode.Err_FRInitialization + ". FR initialization failed. Err: " + e.Message);

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "FR Initialization failed." + Environment.NewLine + "Err: " + e.Message,
                    ErrMsg = "Err Code: " + ErrCode.Err_FRInitialization + ". Err: " + e.Message
                };
                
                ForwardErrMsg(rp);

                return false;
            }
        }
        

        public MainViewModel(IDataService dataService)
        {
            Title = Properties.Settings.Default.TITLE;
            Version = "Ver: " + MyApp.GetVersion();

            TitleVersion = Title + "   " + " { " + version + " } ";
          
            logger.Info("Validating Demo is started.");

            // This is to control camera frame rate.
            timerCameraFrame.Interval = new TimeSpan(0, 0, 0, 0, 40);
            timerCameraFrame.Tick += timerCameraFrame_Tick;

            // Every one second, we will monitor for _isQrRead variable.
            timerQr.Interval = new TimeSpan(0, 0, 1);
            timerQr.Tick += timerQr_Tick;

            // Reset timer to set screen back to reading SQRC after 3 sec.
            timerReset.Interval = new TimeSpan(0, 0, 2);
            timerReset.Tick += timerReset_Tick;

            InitFR();

            HiddenData = "";
            ScanInput = "";

            StartMainJob();
        }

        private void ProcessHiddenData(string incoming, bool fromFr = false)
        {
            var data = incoming.Split(new string[] { "," }, StringSplitOptions.None);

            if (data.Length != 4)
            {
                logger.Warn("Err Code: " + ErrCode.Err_SqrcContents + ". Err: Invalid SQRC contents! Contents: " + incoming);

                // Invalid data.
                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "Invalid SQRC contents!",
                    ErrMsg = "Err Code: " + ErrCode.Err_SqrcContents + "."
                };

                ForwardErrMsg(rp);

                return;
            }

            HiddenData = "Name: " + data[0].Trim() +
                        Environment.NewLine +
                        "Contact: " + data[1].Trim() +
                        Environment.NewLine +
                        Environment.NewLine +
                        "ID No: " + data[2].Trim() +
                        Environment.NewLine +
                        "Pass Expiry Date: " + data[3].Trim();
        }

       
        private void StartMainJob()
        {
            InitGuiScanner();

            // Initialize (any) previous read data from Face SQRC library memory.
            int result = FaceAuthOpt.InitQR();
            if (result != 0)
            {
                logger.Error("Err Code: " + ErrCode.Err_StartMainJob + ". Err: Face SQRC DLL Load error: QR reader initialization failed");

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "FR QR Reader Initialization failed." + Environment.NewLine + "Please check the parameter + setting and restart the application",
                    ErrMsg = "Err Code: " + ErrCode.Err_StartMainJob + ". Err: InitQR failed!"
                };
                
                ForwardErrMsg(rp);
                
                return;
            }

            // Start timerQR.
            timerQr.Start();

            // Reset all variables/parameters.
            userData = "";
            _isQrRead = false;
            _isAuthenticated = false;
            _checkCount = 0;
            _successCount = 0;

            // Abort the thread if it is already started/created.
            if (_thread != null && _thread.IsAlive)
                _thread.Abort();

            Thread.Sleep(500);

            // Define the thread and start it.
            _thread = new Thread(ReadFaceSQRC);
            _thread.Start();
        }

        private void StopAllFrRelated()
        {
            try
            {
                if (_thread != null && _thread.IsAlive)
                    _thread.Abort();

                timerQr.Stop();
                timerCameraFrame.Stop();
                timerReset.Stop();
            }
            catch (Exception e)
            {
                logger.Error("Err Code: " + ErrCode.Err_StopAllFrRelated + ". Failed to stopp all FR services. Err: " + e.Message);

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "Failed to stop all FR services.",
                    ErrMsg = "Err Code: " + ErrCode.Err_StopAllFrRelated + ". Err: " + e.Message
                };

                ForwardErrMsg(rp);
            }
           
        }

        // This timer will monitor for SQRC code read every 1 second.
        // Once the code is successfully read, _isQrRead variable will be true.
        // Then we will start timerCameraFrame and start the verification process.
        private void timerQr_Tick(object sender, EventArgs e)
        {
            // Once the code has been read successfully, stop the timer and start cameraFrameTimer.
            if (_isQrRead)
            {
                // Stop current timer.
                timerQr.Stop();

                ScanResult = "Loading Camera...";

                // Start Camera timer.
                StartCamera();
            }
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
                        FaceVerifyFrame = Mat2BitmapImage(frame);
                        Verifyface(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                timerCameraFrame.Stop();
                logger.Error("Mytimer exception: " + ex.Message);
            }
        }

        private void timerReset_Tick(object sender, EventArgs e)
        {
            // Stop necessary timer.
            timerReset.Stop();
            timerCameraFrame.Stop();

            // Reset Screen.
            InitGuiScanner();

            // Start Reading SQRC.
            StartMainJob();
        }


        // Call ReadQR to wait for user to scan Face SQRC.
        // Call GetUserData to get private data from Face SQRC.
        private void ReadFaceSQRC()
        {
            _isThreadRunning = true;
            _isQrRead = false;

            errMsg = "";

            int result;

            try
            {
                logger.Info("Face SQRC reading is started.");

                userData = "";
                while (_isThreadRunning)
                {
                    Thread.Sleep(100);

                    if (!_isThreadRunning)
                        return;

                    // Read Face SQRC data.
                    result = FaceAuthOpt.ReadQR();

                    switch (result)
                    {
                        case 0: errMsg = ""; logger.Debug("FaceSQRC read success!"); break;
                        case 1: errMsg = "Face SQRC DLL error: Failed to read FaceSQRC (Format error)."; break;
                        case 2: errMsg = "Face SQRC DLL error: Fail to read FaceSQRC (Timeout)."; break;
                        case 3: errMsg = "Face SQRC DLL error: Please initialize Face Authentication dll first before reading Face SQRC!"; break;
                        case 21: errMsg = "Face SQRC DLL error: Different version of Face Recognition data."; break;
                        case 22: errMsg = "Face SQRC DLL error: Face recognition data format error."; break;
                        case 23: errMsg = "Face SQRC DLL error: Face recognition data decryption failure."; break;
                        case 24: errMsg = "Face SQRC DLL error: Face recognition data expired."; break;
                        case 25: errMsg = "Face SQRC DLL error: User data error."; break;
                        case 1000: errMsg = "Face SQRC DLL error: License error!"; break;
                        default: errMsg = "Face SQRC DLL error: System error!"; return;
                    }

                    if (!string.IsNullOrEmpty(errMsg))
                    {
                        logger.Error("Err Code: " + ErrCode.Err_ReadFaceSQRC + ". Err: " + errMsg);

                        // If result = 2 means Timeout. If timeout, reloop, no need to show error.
                        if (result != 2)
                        {
                            _isThreadRunning = false;

                            Reply rp = new Reply
                            {
                                IsOk = false,
                                Msg = "Face SQRC reading failed." + Environment.NewLine + "Err: " + errMsg,
                                ErrMsg = "Err Code: " + ErrCode.Err_ReadFaceSQRC
                            };

                            ForwardErrMsg(rp);
                        }
                    }
                    else if (result == 0)
                    {
                        // If read success, get user data (private data).
                        byte[] userDataArray = FaceAuthOpt.GetUserData();

                        userData = Encoding.GetEncoding("Shift_JIS").GetString(userDataArray);

                        if (string.IsNullOrEmpty(userData))
                        {
                            logger.Debug("Err Code: " + ErrCode.Err_ReadFaceSQRC + ". Face SQRC read successed. However, no hidden data found!");

                            Reply rp = new Reply
                            {
                                IsOk = false,
                                Msg = "Face SQRC read successed. However, no hidden data found",
                                ErrMsg = "Err Code: " + ErrCode.Err_ReadFaceSQRC
                            };

                            ForwardErrMsg(rp);
                        }

                        logger.Debug("FR Hidden Data: " + userData);

                        _isThreadRunning = false;
                    }
                }

                if (!string.IsNullOrEmpty(userData))
                {
                    _isQrRead = true;
                    ProcessHiddenData(userData, true);
                }
                    
            }
            catch (Exception e)
            {
                _isThreadRunning = false;
                if (!e.Message.Contains("aborted"))
                {
                    logger.Debug("Err Code: " + ErrCode.Err_ReadFaceSQRC + ". Face SQRC code reading failed. Err: " + e.Message);
                }
            }
            finally
            {
                _isThreadRunning = false;
            }
        }


        private void StartCamera()
        {
            try
            {
                if (_videoCapture != null)
                    _videoCapture.Release();

                _videoCapture = new VideoCapture(Properties.Settings.Default.CAMERA_NO);

                // Hide SQRC reading image.
                VisibleScannerImage = false;
                VisibleCamera = true;
                VisibleStill = false;
                

                if (_videoCapture.IsOpened())
                {
                    timerCameraFrame.Start();

                    ScanResult = "Scanning Facial Points ...";

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


        // Call VerifyFaceB to identify (verify) face.
        // Call GetFaceRect to get frame around face.
        // Based on SuccessCount rersult, display OK/NG status.
        // Start Reset Timer to refresh screen for next verification.
        private void Verifyface(Bitmap bitmap)
        {
            try
            {
                Console.WriteLine("Verify face: " + DateTime.Now.ToString("hh:mm:ss"));

                if (_checkCount <= Properties.Settings.Default.FACE_CHECK_COUNT &&
                    _successCount <= Properties.Settings.Default.FACE_SUCCESS_COUNT)
                {
                    int result;
                    // To identify current face data with previously read Face SQRC data.
                    result = FaceAuthOpt.VerifyFaceB(bitmap);
                    if (result == 0)
                    {
                        _isAuthenticated = true;
                        _checkCount++;
                        _successCount++;
                    }
                    else
                    {
                        _isAuthenticated = false;
                        _checkCount++;
                    }

                    int[] recFace = { 0, 0, 0, 0 };
                    // To draw rectangle around face to show success/fail verification.
                    result = FaceAuthOpt.GetFaceRect(recFace);
                    if (result != 0)
                    {
                        logger.Error("Err Code: " + ErrCode.Err_Verifyface + ". Face SQRC Dll error: System error(GetFaceRect): result: " + result);
                        return;
                    }

                    // Draw rectangle around face.
                    FaceVerifyFrame = BitmapToBitmapImage(DrawRectangle(recFace, bitmap));

                    // This is for result image frame.
                    FaceVerifyImage = FaceVerifyFrame;
                }
                else
                {
                    timerCameraFrame.Stop();

                    VisibleCamera = false;

                    //// Get still image from last camera frame.
                    //FaceVerifyImage = FaceVerifyFrame;
                    VisibleStill = true;

                    _videoCapture.Release();

                    VisibleFrHiddenData = true;

                    if (_successCount >= Properties.Settings.Default.FACE_SUCCESS_COUNT)
                    {
                        // If successfully authenticated, show OK result.
                        ScanResultOk = true;
                        ScanResultNg = false;
                        ScanResult = "Welcome to DIAS!";
                    }
                    else
                    {
                        // If fail, show NG result.
                        ScanResultOk = false;
                        ScanResultNg = true;
                        ScanResult = "Sorry, You are not authorized!";
                    }

                    // Start reset timer to refresh screen for next face verification.
                    timerReset.Start();
                }
            }
            catch (Exception e)
            {
                logger.Error("Err Code: " + ErrCode.Err_Verifyface + ". Err: Face Verification failed. Details: " + e.Message);

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "Face Verification failed!" + Environment.NewLine +
                         "Err: " + e.Message,
                    ErrMsg = "Err Code: " + ErrCode.Err_Verifyface + ". Err: " + e.Message
                };
                ForwardErrMsg(rp);
            }
        }




        public override void Cleanup()
        {
            try
            {
                timerCameraFrame.Stop();

                if (_thread != null && _thread.IsAlive)
                {
                    _isThreadRunning = false;
                    Thread.Sleep(1000);
                    _thread.Abort();
                }
                _thread = null;

                timerCameraFrame = null;

                timerQr.Stop();
                timerQr = null;

                timerReset.Stop();
                timerReset = null;
                base.Cleanup();
            }
            catch (Exception ex)
            {
                logger.Error("Err Code: " + ErrCode.Err_Cleanup + ". Clean up failed. Details: " + ex.Message);

                Reply rp = new Reply
                {
                    IsOk = false,
                    Msg = "Clean up failed!" + Environment.NewLine +
                         "Err: " + ex.Message,
                    ErrMsg = "Err Code: " + ErrCode.Err_Cleanup + ". Err: " + ex.Message
                };
                ForwardErrMsg(rp);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_videoCapture.IsDisposed == false)
                {
                    _videoCapture.Dispose();
                    _videoCapture = null;
                }
            }
        }
    }
}