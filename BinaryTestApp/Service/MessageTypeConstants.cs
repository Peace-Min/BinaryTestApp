namespace BinaryTestApp.Service
{
    /// <summary>
    /// 메시지 타입별 폴더명 상수 정의
    /// 각 메시지 타입은 별도의 폴더에 저장됩니다.
    /// 모드별(ECS, ICC)로 구분하여 관리합니다.
    /// </summary>
    public static class MessageTypeConstants
    {
        /// <summary>
        /// ECS 모드 메시지 타입
        /// </summary>
        public static class ECS
        {
            public const string MsgModel = "ECS_MsgModel";
            // 추가 메시지 타입:
            // public const string Msg0 = "ECS_Msg0";
            // public const string Msg1 = "ECS_Msg1";
        }

        /// <summary>
        /// ICC 모드 메시지 타입
        /// </summary>
        public static class ICC
        {
            public const string MsgModel = "ICC_MsgModel";
            // 추가 메시지 타입:
            // public const string Msg0 = "ICC_Msg0";
            // public const string Msg1 = "ICC_Msg1";
        }
    }
}

