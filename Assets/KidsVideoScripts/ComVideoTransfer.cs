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
        protected string signalingUrl = @"ws://signaling.because-why-not.com/test";
        [SerializeField]
        protected InputField inputField = null;
        protected string address = string.Empty;
        protected NetworkConfig netConfig = null;
        protected ICall caller = null;

        protected void AddStepLog(string fmt,params object[] argv)
        {
            var value = string.Format(fmt, argv);
#if UNITY_EDITOR
            Debug.LogFormat("<color=#00ff00>[video_step]:{0}</color>", value);
#else
            Debug.LogFormat("[video_step]:{0}", value);
#endif
        }

        public void StartAsTeacher()
        {
            AddStepLog("StartAsTeacher");
            ConfigAsTeacher();
        }

        public void StartAsStudent()
        {
            AddStepLog("StartAsStudent");
        }

        protected void ConfigAsTeacher()
        {
            AddStepLog("ConfigNet");

            if (null == UnityCallFactory.Instance)
            {
                //if it is null something went terribly wrong
                AddStepLog("UnityCallFactory missing. Platform not supported / dll's missing?");
                return;
            }

            if(null == inputField)
            {
                AddStepLog("inputField has not been assigned ...");
                return;
            }

            if(string.IsNullOrEmpty(inputField.text) || inputField.text.Length != 4)
            {
                AddStepLog("verify code is error need length = 4 such as 9527");
                return;
            }

            netConfig = new NetworkConfig();
            netConfig.SignalingUrl = signalingUrl;

            //Set a stun server as ice server. We use a free google stun
            //server here. (blocked in China)
            //This is used by WebRTC to open a port in your router to allow 
            //incoming connections. (not all routers support this though and
            //some firewalls block it)
            netConfig.IceServers.Add(new IceServer("stun:stun.l.google.com:19302"));

            caller = UnityCallFactory.Instance.Create(netConfig);
            if (caller == null)
            {
                //this might happen if our configuration is invalid e.g. broken stun server url
                //(it won't notice if the stun server is offline though)
                AddStepLog("Call init failed");
                return;
            }
            AddStepLog("Call object created");

            caller.CallEvent += reciever_cb;

            AddStepLog("receiver setup");
            //receiver doesn't use video and audio
            MediaConfig mediaConf1 = new MediaConfig();
            mediaConf1.Video = true;
            mediaConf1.Audio = true;
            mediaConf1.IdealWidth = 1280;
            mediaConf1.IdealHeight = 720;

            caller.Configure(mediaConf1);
        }

        private void reciever_cb(object sender, CallEventArgs args)
        {
            if (args.Type == CallEventType.ConfigurationComplete)
            {
                //STEP3: configuration completed -> try calling
                Call(false);
            }
            else if (args.Type == CallEventType.ConfigurationFailed)
            {
                AddStepLog("Accessing audio / video failed");
            }
            else if (args.Type == CallEventType.ConnectionFailed)
            {
                AddStepLog("ConnectionFailed");
            }
            else if (args.Type == CallEventType.ListeningFailed)
            {
                AddStepLog("ListeningFailed");
            }
            else if (args.Type == CallEventType.CallAccepted)
            {
                //STEP5: We are connected
                //mState = SimpleCallState.InCall;
                AddStepLog("Connection established");
            }
            else if (args.Type == CallEventType.CallEnded)
            {
                //mState = SimpleCallState.Ended;
                AddStepLog("Call ended.");
            }
            else if (args.Type == CallEventType.FrameUpdate)
            {
                //STEP6: until the end of the call we receive frames here
                //Note that this is being called after Configure already for local frames even before
                //a connection is established!
                //This is triggered each video frame for local and remote video images
                FrameUpdateEventArgs frameArgs = args as FrameUpdateEventArgs;


                if (frameArgs.ConnectionId == ConnectionId.INVALID)
                {
                    /*
                    bool textureCreated = UnityMediaHelper.UpdateRawImage(_LocalImage, frameArgs.Frame);
                    if (textureCreated)
                    {
                        Texture2D tex = _LocalImage.texture as Texture2D;
                        AddStepLog("Local Texture(s) created " + tex.width + "x" + tex.height + " format: " + frameArgs.Frame.Format);
                    }*/

                }
                else
                {
                    /*
                    bool textureCreated = UnityMediaHelper.UpdateRawImage(_RemoteImage, frameArgs.Frame);
                    if (textureCreated)
                    {
                        Texture2D tex = _RemoteImage.texture as Texture2D;
                        AddStepLog("Remote Texture(s) created " + tex.width + "x" + tex.height + " format: " + frameArgs.Frame.Format);
                    }
                    */
                }
            }
        }

        private void Call(bool _Sender)
        {
            var address = string.Format(compareNameFmt, inputField.text);

            if (_Sender)
            {
                //STEP4: Sender calls (outgoing connection) 
                caller.Call(address);
            }
            else
            {
                //STEP4: Receiver listens (waiting for incoming connection)
                caller.Listen(address);
                AddStepLog("reciever listen address = {0}", address);
            }
            //mState = SimpleCallState.Calling;
        }
    }
}