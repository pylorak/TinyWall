using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pylorak.Windows.WFP
{
    public static class BuiltinCallouts
    {
        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_TRANSPORT_V4 = new Guid(
            0x5132900d,
            0x5e84,
            0x4b5f,
            0x80, 0xe4, 0x01, 0x74, 0x1e, 0x81, 0xff, 0x10);

        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_TRANSPORT_V6 = new Guid(
            0x49d3ac92,
            0x2a6c,
            0x4dcf,
            0x95, 0x5f, 0x1c, 0x3b, 0xe0, 0x09, 0xdd, 0x99);

        public static readonly Guid FWPM_CALLOUT_IPSEC_OUTBOUND_TRANSPORT_V4 = new Guid(
            0x4b46bf0a,
            0x4523,
            0x4e57,
            0xaa, 0x38, 0xa8, 0x79, 0x87, 0xc9, 0x10, 0xd9);

        public static readonly Guid FWPM_CALLOUT_IPSEC_OUTBOUND_TRANSPORT_V6 = new Guid(
            0x38d87722,
            0xad83,
            0x4f11,
            0xa9, 0x1f, 0xdf, 0x0f, 0xb0, 0x77, 0x22, 0x5b);

        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_TUNNEL_V4 = new Guid(
            0x191a8a46,
            0x0bf8,
            0x46cf,
            0xb0, 0x45, 0x4b, 0x45, 0xdf, 0xa6, 0xa3, 0x24);

        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_TUNNEL_V6 = new Guid(
            0x80c342e3,
            0x1e53,
            0x4d6f,
            0x9b, 0x44, 0x03, 0xdf, 0x5a, 0xee, 0xe1, 0x54);

        public static readonly Guid FWPM_CALLOUT_IPSEC_OUTBOUND_TUNNEL_V4 = new Guid(
            0x70a4196c,
            0x835b,
            0x4fb0,
            0x98, 0xe8, 0x07, 0x5f, 0x4d, 0x97, 0x7d, 0x46);

        public static readonly Guid FWPM_CALLOUT_IPSEC_OUTBOUND_TUNNEL_V6 = new Guid(
            0xf1835363,
            0xa6a5,
            0x4e62,
            0xb1, 0x80, 0x23, 0xdb, 0x78, 0x9d, 0x8d, 0xa6);

        public static readonly Guid FWPM_CALLOUT_IPSEC_FORWARD_INBOUND_TUNNEL_V4 = new Guid(
            0x28829633,
            0xc4f0,
            0x4e66,
            0x87, 0x3f, 0x84, 0x4d, 0xb2, 0xa8, 0x99, 0xc7);

        public static readonly Guid FWPM_CALLOUT_IPSEC_FORWARD_INBOUND_TUNNEL_V6 = new Guid(
            0xaf50bec2,
            0xc686,
            0x429a,
            0x88, 0x4d, 0xb7, 0x44, 0x43, 0xe7, 0xb0, 0xb4);

        public static readonly Guid FWPM_CALLOUT_IPSEC_FORWARD_OUTBOUND_TUNNEL_V4 = new Guid(
            0xfb532136,
            0x15cb,
            0x440b,
            0x93, 0x7c, 0x17, 0x17, 0xca, 0x32, 0x0c, 0x40);

        public static readonly Guid FWPM_CALLOUT_IPSEC_FORWARD_OUTBOUND_TUNNEL_V6 = new Guid(
            0xdae640cc,
            0xe021,
            0x4bee,
            0x9e, 0xb6, 0xa4, 0x8b, 0x27, 0x5c, 0x8c, 0x1d);

        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_INITIATE_SECURE_V4 = new Guid(
            0x7dff309b,
            0xba7d,
            0x4aba,
            0x91, 0xaa, 0xae, 0x5c, 0x66, 0x40, 0xc9, 0x44);

        public static readonly Guid FWPM_CALLOUT_IPSEC_INBOUND_INITIATE_SECURE_V6 = new Guid(
            0xa9a0d6d9,
            0xc58c,
            0x474e,
            0x8a, 0xeb, 0x3c, 0xfe, 0x99, 0xd6, 0xd5, 0x3d);

        public static readonly Guid FWPM_CALLOUT_IPSEC_ALE_CONNECT_V4 = new Guid(
            0x6ac141fc,
            0xf75d,
            0x4203,
            0xb9, 0xc8, 0x48, 0xe6, 0x14, 0x9c, 0x27, 0x12);

        public static readonly Guid FWPM_CALLOUT_IPSEC_ALE_CONNECT_V6 = new Guid(
            0x4c0dda05,
            0xe31f,
            0x4666,
            0x90, 0xb0, 0xb3, 0xdf, 0xad, 0x34, 0x12, 0x9a);

        public static readonly Guid FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V4_SILENT_DROP = new Guid(
            0xeda08606,
            0x2494,
            0x4d78,
            0x89, 0xbc, 0x67, 0x83, 0x7c, 0x03, 0xb9, 0x69);

        public static readonly Guid FWPM_CALLOUT_WFP_TRANSPORT_LAYER_V6_SILENT_DROP = new Guid(
            0x8693cc74,
            0xa075,
            0x4156,
            0xb4, 0x76, 0x92, 0x86, 0xee, 0xce, 0x81, 0x4e);

        public static readonly Guid FWPM_CALLOUT_TCP_CHIMNEY_CONNECT_LAYER_V4 = new Guid(
            0xf3e10ab3,
            0x2c25,
            0x4279,
            0xac, 0x36, 0xc3, 0x0f, 0xc1, 0x81, 0xbe, 0xc4);

        public static readonly Guid FWPM_CALLOUT_TCP_CHIMNEY_CONNECT_LAYER_V6 = new Guid(
            0x39e22085,
            0xa341,
            0x42fc,
            0xa2, 0x79, 0xae, 0xc9, 0x4e, 0x68, 0x9c, 0x56);

        public static readonly Guid FWPM_CALLOUT_TCP_CHIMNEY_ACCEPT_LAYER_V4 = new Guid(
            0xe183ecb2,
            0x3a7f,
            0x4b54,
            0x8a, 0xd9, 0x76, 0x05, 0x0e, 0xd8, 0x80, 0xca);

        public static readonly Guid FWPM_CALLOUT_TCP_CHIMNEY_ACCEPT_LAYER_V6 = new Guid(
            0x0378cf41,
            0xbf98,
            0x4603,
            0x81, 0xf2, 0x7f, 0x12, 0x58, 0x60, 0x79, 0xf6);
    }
}
