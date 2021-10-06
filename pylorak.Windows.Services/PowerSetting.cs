using System;

namespace pylorak.Windows.Services
{
    public static class PowerSetting
    {
        // 5D3E9A59-E9D5-4B00-A6BD-FF34FF516548
        public static readonly Guid GUID_ACDC_POWER_SOURCE = new Guid(
            0x5D3E9A59, 0xE9D5, 0x4B00, 0xA6, 0xBD, 0xFF, 0x34, 0xFF, 0x51, 0x65, 0x48
        );

        // A7AD8041-B45A-4CAE-87A3-EECBB468A9E1
        public static readonly Guid GUID_BATTERY_PERCENTAGE_REMAINING = new Guid(
            0xA7AD8041, 0xB45A, 0x4CAE, 0x87, 0xA3, 0xEE, 0xCB, 0xB4, 0x68, 0xA9, 0xE1
        );

        // 6FE69556-704A-47A0-8F24-C28D936FDA47
        public static readonly Guid GUID_CONSOLE_DISPLAY_STATE = new Guid(
            0x6fe69556, 0x704a, 0x47a0, 0x8f, 0x24, 0xc2, 0x8d, 0x93, 0x6f, 0xda, 0x47
        );

        // 786E8A1D-B427-4344-9207-09E70BDCBEA9
        public static readonly Guid GUID_GLOBAL_USER_PRESENCE = new Guid(
            0x786e8a1d, 0xb427, 0x4344, 0x92, 0x7, 0x9, 0xe7, 0xb, 0xdc, 0xbe, 0xa9
        );

        // 515C31D8-F734-163D-A0FD-11A08C91E8F1
        public static readonly Guid GUID_IDLE_BACKGROUND_TASK = new Guid(
            0x515C31D8, 0xF734, 0x163D, 0xA0, 0xFD, 0x11, 0xA0, 0x8C, 0x91, 0xE8, 0xF1
        );

        // 02731015-4510-4526-99E6-E5A17EBD1AEA
        public static readonly Guid GUID_MONITOR_POWER_ON = new Guid(
            0x02731015, 0x4510, 0x4526, 0x99, 0xE6, 0xE5, 0xA1, 0x7E, 0xBD, 0x1A, 0xEA
        );

        // E00958C0-C213-4ACE-AC77-FECCED2EEEA5
        public static readonly Guid GUID_POWER_SAVING_STATUS = new Guid(
            0xe00958c0, 0xc213, 0x4ace, 0xac, 0x77, 0xfe, 0xcc, 0xed, 0x2e, 0xee, 0xa5
        );

        // 245d8541-3943-4422-b025-13A784F679B7
        public static readonly Guid GUID_POWERSCHEME_PERSONALITY = new Guid(
            0x245D8541, 0x3943, 0x4422, 0xB0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7
        );

        // 3C0F4548-C03F-4c4d-B9F2-237EDE686376
        public static readonly Guid GUID_SESSION_USER_PRESENCE = new Guid(
            0x3c0f4548, 0xc03f, 0x4c4d, 0xb9, 0xf2, 0x23, 0x7e, 0xde, 0x68, 0x63, 0x76
        );

        // BA3E0F4D-B817-4094-A2D1-D56379E6A0F3
        public static readonly Guid GUID_LIDSWITCH_STATE_CHANGE = new Guid(
            0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0, 0xF3
        );

        // 98A7F580-01F7-48AA-9C0F-44352C29E5C0
        public static readonly Guid GUID_SYSTEM_AWAYMODE = new Guid(
            0x98A7F580, 0x01F7, 0x48AA, 0x9C, 0x0F, 0x44, 0x35, 0x2C, 0x29, 0xE5, 0xC0
        );
    }
}
