

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MTConnectAgentCore
{
    class MTConnectNameSpace
    {
        //public static XNamespace mtError = "urn:mtconnect.com:MTConnectError:0.9";
        public static XNamespace mtError = "urn:mtconnect.org:MTConnectError:1.1";
        //public static XNamespace mtStreams = "urn:mtconnect.com:MTConnectStreams:0.9";
        public static XNamespace mtStreams = "urn:mtconnect.org:MTConnectStreams:1.1";
        //public static XNamespace mtDevices = "urn:mtconnect.com:MTConnectDevices:0.9";
        public static XNamespace mtDevices = "urn:mtconnect.org:MTConnectDevices:1.1";

        public static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        //used in namespacemanager

        //public static String mtConnectUriDevices = "urn:mtconnect.com:MTConnectDevices:0.9";
        public static String mtConnectUriDevices = "urn:mtconnect.org:MTConnectDevices:1.1";
        //public static String mtConnectUriStreams = "urn:mtconnect.com:MTConnectStreams:0.9";
        public static String mtConnectUriStreams = "urn:mtconnect.org:MTConnectStreams:1.1";
        public static String mtConnectUriError = "urn:mtconnect.org:MTConnectError:1.1";

        public static String mtConnectPrefix = "mt";
    }
}
