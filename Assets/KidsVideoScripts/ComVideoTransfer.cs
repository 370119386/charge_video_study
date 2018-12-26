using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Byn.Media;
using Byn.Common;
using UnityEngine.UI;
using Byn.Net;

namespace KidsVideo
{
    //kids video component
    public class ComVideoTransfer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip(@"视频聊天通道地址")]
        protected string compareNameFmt = @"au_video_room_{0}";
        [SerializeField]
        [Tooltip(@"信道服务器地址")]
        //Note: The conference uses a different server setup which allows multiple clients listening on
        //the same address and receive every connection listening.
        protected string mSignalingUrl = "wss://signaling.because-why-not.com/conferenceapp";
        [SerializeField]
        protected InputField inputField = null;
        protected string address = string.Empty;
        [Tooltip(@"防火墙穿透服务器")]
        /// <summary>
        /// ice server server. Used to get trough the firewall and establish direct connections.
        /// </summary>
        protected IceServer mIceServer = new IceServer("turn:turn.because-why-not.com:443", "testuser14", "pass14");

        protected NetworkConfig netConfig = null;
        /// <summary>
        /// Call class handling all the functionality
        /// </summary>
        private ICall mCall;

        /// <summary>
        /// Configuration of audio / video functionality
        /// </summary>
        private MediaConfig config = new MediaConfig();

        [SerializeField]
        protected ComVideo comLargeVideo = null;
        [SerializeField]
        protected ComVideo comSmallVideo = null;

        protected void AddStepLog(string fmt,params object[] argv)
        {
            var value = string.Format(fmt, argv);
#if UNITY_EDITOR
            Debug.LogFormat("<color=#00ff00>[video_step]:{0}</color>", value);
#else
            Debug.LogFormat("[video_step]:{0}", value);
#endif
        }

        protected void Start()
        {
            config.Video = true;
            config.Audio = true;
        }
        
        /// <summary>
        /// The call object needs to be updated regularly to sync data received via webrtc with
        /// unity. All events will be triggered during the update method in the unity main thread
        /// to avoid multi threading errors
        /// </summary>
        private void Update()
        {
            if (mCall != null)
            {
                //update the call
                mCall.Update();
            }
        }

        protected void OnDestroy()
        {
            CleanupCall();
        }

        private void Setup(bool useAudio = true, bool useVideo = true)
        {
            AddStepLog("Setting up ...");

            //setup the server
            NetworkConfig netConfig = new NetworkConfig();
            netConfig.IceServers.Add(mIceServer);
            netConfig.SignalingUrl = mSignalingUrl;
            netConfig.IsConference = true;
            mCall = UnityCallFactory.Instance.Create(netConfig);
            if (mCall == null)
            {
                AddStepLog("Failed to create the call");
                return;
            }

            AddStepLog("Call created!");
            mCall.CallEvent += Call_CallEvent;

            //setup local video element
            SetupVideoUi(ConnectionId.INVALID);
            mCall.Configure(config);


            //SetGuiState(false);
        }

        /// <summary>
        /// Creates the connection specific data / ui
        /// </summary>
        /// <param name="id"></param>
        private void SetupVideoUi(ConnectionId id)
        {
            AddStepLog("new connectionId coming id = {0}", id.id);
            //create texture + ui element
            if(id == ConnectionId.INVALID)
            {
                comLargeVideo.gameObject.SetActive(true);
                comLargeVideo.rawImage.texture = comLargeVideo.defaultTexture;
            }
            else
            {
                comSmallVideo.gameObject.SetActive(true);
                comSmallVideo.rawImage.texture = comSmallVideo.defaultTexture;
            }
            /*
            VideoData vd = new VideoData();
            vd.uiObject = Instantiate(uVideoPrefab);
            vd.uiObject.transform.SetParent(uVideoLayout.transform, false);
            vd.image = vd.uiObject.GetComponentInChildren<RawImage>();
            vd.image.texture = uNoImgTexture;
            mVideoUiElements[id] = vd;
            */
        }

        /// <summary>
        /// Handler of call events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Call_CallEvent(object sender, CallEventArgs e)
        {
            switch (e.Type)
            {
                case CallEventType.CallAccepted:
                    //Outgoing call was successful or an incoming call arrived
                    AddStepLog("Connection established");
                    OnNewCall(e as CallAcceptedEventArgs);
                    break;
                case CallEventType.CallEnded:
                    OnCallEnded(e as CallEndedEventArgs);
                    break;
                case CallEventType.ListeningFailed:
                    AddStepLog("Failed to listen for incoming calls! Server might be down!");
                    ResetCall();
                    break;

                case CallEventType.ConnectionFailed:
                    {
                        //this should be impossible to happen in conference mode!
                        Byn.Media.ErrorEventArgs args = e as Byn.Media.ErrorEventArgs;
                        AddStepLog("Error: " + args.ErrorMessage);
                        Debug.LogError(args.ErrorMessage);
                        ResetCall();
                    }
                    break;

                case CallEventType.FrameUpdate:
                    //new frame received from webrtc (either from local camera or network)
                    FrameUpdateEventArgs frameargs = e as FrameUpdateEventArgs;
                    UpdateFrame(frameargs.ConnectionId, frameargs.Frame);
                    break;
                case CallEventType.Message:
                    {
                        //text message received
                        MessageEventArgs args = e as MessageEventArgs;
                        AddStepLog(args.Content);
                        break;
                    }
                case CallEventType.WaitForIncomingCall:
                    {
                        //the chat app will wait for another app to connect via the same string
                        WaitForIncomingCallEventArgs args = e as WaitForIncomingCallEventArgs;
                        AddStepLog("Waiting for incoming call address: " + args.Address);
                        break;
                    }
            }

        }

        /// <summary>
        /// Destroys the call object and shows the setup screen again.
        /// Called after a call ends or an error occurred.
        /// </summary>
        private void ResetCall()
        {
            //delete all call object
            /*
            foreach (var v in mVideoUiElements)
            {
                Destroy(v.Value.uiObject);
                Destroy(v.Value.texture);
            }*/
            //mVideoUiElements.Clear();
            comLargeVideo.gameObject.SetActive(false);
            comSmallVideo.gameObject.SetActive(false);
            CleanupCall();
            //change state
            //SetGuiState(true);
        }

        /// <summary>
        /// Destroys the call. Used if unity destroys the object or if a call
        /// ended / failed due to an error.
        /// 
        /// </summary>
        private void CleanupCall()
        {
            if (mCall != null)
            {
                AddStepLog("Destroying call!");
                mCall.Dispose();
                mCall = null;
                AddStepLog("Call destroyed");
            }
        }

        /// <summary>
        /// User left. Cleanup connection specific data / ui
        /// </summary>
        /// <param name="args"></param>
        private void OnCallEnded(CallEndedEventArgs args)
        {
            //delete call object
            /*
            VideoData data;
            if (mVideoUiElements.TryGetValue(args.ConnectionId, out data))
            {
                Destroy(data.texture);
                Destroy(data.uiObject);
                mVideoUiElements.Remove(args.ConnectionId);
            }*/
        }

        /// <summary>
        /// Event triggers for a new incoming call
        /// (in conference mode there is no difference between incoming / outgoing)
        /// </summary>
        /// <param name="args"></param>
        private void OnNewCall(CallAcceptedEventArgs args)
        {
            SetupVideoUi(args.ConnectionId);
        }

        /// <summary>
        /// Updates the texture based on the given frame update.
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="frame"></param>
        private void UpdateTexture(ref Texture2D tex, IFrame frame)
        {
            //texture exists but has the wrong height /width? -> destroy it and set the value to null
            if (tex != null && (tex.width != frame.Width || tex.height != frame.Height))
            {
                Texture2D.Destroy(tex);
                tex = null;
            }
            //no texture? create a new one first
            if (tex == null)
            {
                tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
            }
            ///copy image data into the texture and apply
            tex.LoadRawTextureData(frame.Buffer);
            tex.Apply();
        }

        /// <summary>
        /// Updates the frame for a connection id. If the id is new it will create a
        /// visible image for it. The frame can be null for connections that
        /// don't sent frames.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="frame"></param>
        private void UpdateFrame(ConnectionId id, IFrame frame)
        {
            AddStepLog("UpdateFrame ...");
            if(id == ConnectionId.INVALID)
            {
                UpdateTexture(ref comLargeVideo.texture, frame);
                comLargeVideo.rawImage.texture = comLargeVideo.texture;
            }
            else
            {
                UpdateTexture(ref comSmallVideo.texture, frame);
                comSmallVideo.rawImage.texture = comSmallVideo.texture;
            }
            //if (mVideoUiElements.ContainsKey(id))
            //{
            //    VideoData videoData = mVideoUiElements[id];
            //    UpdateTexture(ref videoData.texture, frame);
            //    videoData.image.texture = videoData.texture;
            //}
        }

        /// <summary>
        /// Join button pressed. Tries to join a room.
        /// </summary>
        public void JoinButtonPressed()
        {
            address = string.Format(compareNameFmt, inputField.text);
            if(string.IsNullOrEmpty(inputField.text))
            {
                AddStepLog("VerifyCode Error : Now is empty");
                return;
            }

            if(inputField.text.Length != 4)
            {
                AddStepLog("VerifyCode Length Error : Format Just Like 9527 4 numbers");
                return;
            }

            if (mCall == null)
            {
                Setup();
                //EnsureLength();
                if(null != mCall)
                {
                    AddStepLog("Start Listen Address:[{0}]",address);
                    mCall.Listen(address);
                }
            }
        }
    }
}