using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KidsVideo
{
    public enum VideoType
    {
        VT_SELF = 0,
        VT_OTHER = 1,
    }

    public class ComVideo : MonoBehaviour
    {
        public Texture2D defaultTexture;
        public Texture2D texture;
        public RawImage rawImage;
    }
}