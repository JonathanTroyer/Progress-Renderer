namespace ProgressRenderer.Source.Enum
{
    public static class EnumUtils
    {
        public static string GetFileExtension(EncodingType type)
        {
            switch (type)
            {
                case EncodingType.UnityJPG:
                default:
                    return "jpg";
                case EncodingType.UnityPNG:
                    return "png";
            }
        }

        public static string ToFriendlyString(EncodingType type)
        {
            switch (type)
            {
                case EncodingType.UnityJPG:
                default:
                    return "JPG_unity";
                case EncodingType.UnityPNG:
                    return "PNG_unity";
            }
        }

        public static string ToFriendlyString(JPGQualityAdjustmentSetting type)
        {
            switch (type)
            {
                case JPGQualityAdjustmentSetting.Manual:
                default:
                    return "Manual";
                case JPGQualityAdjustmentSetting.Automatic:
                    return "Automatic";
            }
        }

        public static string ToFriendlyString(FileNamePattern type)
        {
            switch (type)
            {
                case FileNamePattern.DateTime:
                default:
                    return "DateTime";
                case FileNamePattern.Numbered:
                    return "Numbered";
                case FileNamePattern.BothTmpCopy:
                    return "BothTmpCopy";
            }

        }

        public static string ToFriendlyString(RenderFeedback type)
        {
            switch (type)
            {
                case RenderFeedback.None:
                    return "None";
                case RenderFeedback.Message:
                default:
                    return "Message";
                case RenderFeedback.Window:
                    return "Window";
            }
        }
    }
}
