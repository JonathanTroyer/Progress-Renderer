namespace ProgressRenderer.Source.Enum
{
    public static class EnumUtils
    {
        public static string GetFileExtension(EncodingType type)
        {
            switch(type) {
                case EncodingType.UnityJPG:
                    return "jpg";
                case EncodingType.UnityPNG:
                    return "png";
            }
            return "jpg";
        }
        public static string ToFriendlyString(EncodingType type)
        {
            switch(type)
            {
                case EncodingType.UnityJPG:
                    return "JPG_unity";
                case EncodingType.UnityPNG:
                    return "PNG_unity";
            }
            return "`JPG_unity";
        }

        public static string ToFriendlyString(JPGQualityAdjustmentSetting type)
        {
            switch (type)
            {
                case JPGQualityAdjustmentSetting.Manual:
                    return "Manual";
                case JPGQualityAdjustmentSetting.Automatic:
                    return "Automatic";
            }
            return "Manual";
        }

        public static string ToFriendlyString(FileNamePattern type)
        {
            switch (type)
            {
                case FileNamePattern.DateTime:
                    return "DateTime";
                case FileNamePattern.Numbered:
                    return "Numbered";
                case FileNamePattern.BothTmpCopy:
                    return "BothTmpCopy";
            }
            return "DateTime";
        }

        public static string ToFriendlyString(RenderFeedback type)
        {
            switch (type)
            {
                case RenderFeedback.None:
                    return "None";
                case RenderFeedback.Message:
                    return "Message";
                case RenderFeedback.Window:
                    return "Window";
            }
            return "Message";
        }
    }
}
