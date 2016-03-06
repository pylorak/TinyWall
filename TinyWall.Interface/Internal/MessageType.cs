namespace TinyWall.Interface.Internal
{
    // Possible message types from controller to service
    public enum MessageType
    {
        // General responses
        INVALID_COMMAND,
        PING,
        RESPONSE_OK,
        RESPONSE_WARNING,
        RESPONSE_ERROR,
        RESPONSE_LOCKED,
        COM_ERROR,

        // Read commands (>31)
        GET_SETTINGS = 32,
        GET_PROCESS_PATH,
        VERIFY_KEYS,
        READ_FW_LOG,
        IS_LOCKED,

        // Unprivileged write commands (>1023)
        UNLOCK = 1024,

        // Privileged write commands (>2047)
        MODE_SWITCH = 2048,
        REINIT,
        PUT_SETTINGS,
        LOCK,
        SET_PASSPHRASE,
        STOP_DISABLE,
        STOP_SERVICE,
        MINUTE_TIMER,
        TEST_EXCEPTION,


        // Service-to-client messages
        DATABASE_UPDATED

    }
}
