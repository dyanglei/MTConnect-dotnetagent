

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace MTConnectAgentCore
{
    public class Util
    {
        public static String[] alarmSeverityOptions = {"CRITICAL", "ERROR", "WARNING", "INFO" };
        public static String[] alarmStateOptions = { "ACTIVE", "CLEARED", "INSTANT"};
        public static String[] alarmCodeOptions = { "FAILURE", "FAULT", "CRASH", "JAM", "OVERLOAD", "ESTOP", "MATERIAL", "MESSAGE", "OTHER" };
        //
        public static String[] typesofCondition = { "Normal","Warning","Fault","Unavailable"};
           
        //
        /*public static String GetAlarmSeverityOptions()
        {
            return ToString(alarmSeverityOptions);
        }*/
        public static String GetAlarmStateOptions()
        {
            return ToString(alarmStateOptions);
        }
        public static String GetAlarmCodeOptions()
        {
            return ToString(alarmCodeOptions);
        }

        //This method convert dvalue from nativeUnits to units if they are different
        //this code is not tested.
        public static double convertUnit(String _dataItemName, double dvalue, String nativeUnits, String units )
        {
            if (units.Equals("DEGREE"))
            {
                if (nativeUnits.Equals("RADIAN"))
                    return convertRADIAN__DEGREE(dvalue);
             }
            else if (units.Equals("CELSIUS"))
            {
                if ( nativeUnits.Equals("FAHRENHEIT" ))
                     return convertFAHRENHEIT__CELSIUS(dvalue);
            }
            else if (units.Equals("MILLIMETER"))
            {
                if (nativeUnits.Equals("INCH"))
                    return convertINCH__MILLIMETER(dvalue);
                else if (nativeUnits.Equals("FOOT"))
                    return convertFOOT__MILLIMETER(dvalue);
            }
            else if (units.Equals("MILLIMETER/SECOND"))
            {
                if (nativeUnits.Equals("INCH/MINUTE"))
                    return convert_Per_MINITUE_To_Per_SECOND((convertINCH__MILLIMETER(dvalue)));
                else if (nativeUnits.Equals("FOOT/MINUTE"))
                    return convert_Per_MINITUE_To_Per_SECOND((convertFOOT__MILLIMETER(dvalue)));
                else if (nativeUnits.Equals("INCH/SECOND"))
                    return convertINCH__MILLIMETER(dvalue);
                else if (nativeUnits.Equals("FOOT/SECOND"))
                    return convertFOOT__MILLIMETER(dvalue);
                else if (nativeUnits.Equals("MILLIMETER/MINUTE"))
                    return convert_Per_MINITUE_To_Per_SECOND(dvalue);
            }
            else if (units.Equals("DEGREE/SECOND"))
            {
                if (nativeUnits.Equals("RADIAN/SECOND"))
                    return convertRADIAN__DEGREE(dvalue);
                else if (nativeUnits.Equals("RADIAN/MINUTE"))
                    return convert_Per_MINITUE_To_Per_SECOND(convertRADIAN__DEGREE(dvalue));
                else if (nativeUnits.Equals("DEGREE/MINUTE"))
                    return convert_Per_MINITUE_To_Per_SECOND(dvalue);
            }
            else if (units.Equals("MILLIMETER/SECOND^2"))
            {
                if (nativeUnits.Equals("INCH/SECOND^2"))
                    return convertINCH__MILLIMETER(dvalue);
                else if (nativeUnits.Equals("FOOT/SECOND^2"))
                    return convertFOOT__MILLIMETER(dvalue);
             }
            else if (units.Equals("DEGREE/SECOND^2"))
            {
                if (nativeUnits.Equals("RADIAN/SECOND^2"))
                    return convertRADIAN__DEGREE(dvalue);
             }        
                 
            else if (units.Equals("KILOGRAM"))
            {
                if (nativeUnits.Equals("POUND"))
                    return convertPOUND__KILOGRAM(dvalue);
             }
            
            else if (units.Equals("REVOLUTION/MINUTE"))
            {
                if (nativeUnits.Equals("REVOLUTION/SECOND"))
                    return convert_Per_SECOND_To_Per_MINUTE(dvalue);
            }
           
         
           
            return dvalue;
        }

        private static double convert_Per_MINITUE_To_Per_SECOND(double pmin)
        {
            return pmin / 60;
        }

        private static double convert_Per_SECOND_To_Per_MINUTE(double sec)
        {
            return sec* 60;
        }

        private static double convertPOUND__KILOGRAM(double lb)
        {
            return lb / 2.2046226218487756;
        }
        private static double convertINCH__MILLIMETER(double inch)
        {
            return inch* 25.4;
        }
        private static double convertFOOT__MILLIMETER(double ft)
        {
            return ft * 12 * 25.4;
        }
        private static double convertFAHRENHEIT__CELSIUS(double f)
        {
            return (5 / 9 * (f - 32));
        }
              
        private static double convertRADIAN__DEGREE(double radian)
        {
            return (radian * 180 / Math.PI);
        }

        private static String ToString(String[] options)
        {
            String r = null;
            foreach (String o in options)
            {
                if (r != null)
                    r = r+ ", " + o;
                else
                    r = o;
            }
            return r;
        }

        private static bool CheckOptions(String[] options, String value)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Equals(value))
                    return true;
            }
            return false;
        }

        public static bool CheckTypesOfCondition(String v)
        {
            return CheckOptions(typesofCondition, v);
        }

        public static bool CheckAlarmServirity(String v)
        {
            return CheckOptions(alarmSeverityOptions, v);
        }
        public static bool CheckAlarmState(String v)
        {
            return CheckOptions(alarmStateOptions, v);
        }
        public static bool CheckAlarmCode(String v)
        {
            return CheckOptions(alarmCodeOptions, v);
        }

        //http://ip/deviceid/probe then get return deviceid
        public static String getFirst(String rawUrl)
        {
            int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
            if  (index == -1 )
                return rawUrl.Substring(1, rawUrl.Length - 1);
            else
                return rawUrl.Substring(1, index - 1);
        }
        
        public static String getSecond(String rawUrl)
        {
            int index = rawUrl.IndexOf("/", 1); //find 2nd '/'
            if (index == -1)
                return null;
            else
                return rawUrl.Substring(index + 1);
        }
        public static XElement createDeviceXST()
        {
            return createXST("MTConnectDevices");
        }
        public static XElement createErrorXST()
        {
            return createXST("MTConnectError");
        }
        public static XElement createStreamXST()
        {
            return createXST("MTConnectStreams");
        }
        private static XElement createXST(String elementName)
        {
            XElement root = null;
            if (elementName.Equals("MTConnectStreams"))
            {
                if (Data.prefix != null)
                {
                    root = new XElement(MTConnectNameSpace.mtStreams + elementName,
                        
                 new XAttribute(XNamespace.Xmlns + Data.prefix, "urn:mtconnect.org:MTConnectStreams:1.1"), new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectStreams:1.1 http://mtconnect.org/schemas/MTConnectStreams_1.1.xsd"));
                }
                else
                { 
                    root = new XElement(MTConnectNameSpace.mtStreams + elementName,
                        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectStreams:1.1 http://mtconnect.org/schemas/MTConnectStreams_1.1.xsd"));
                }
               
            }
            else if (elementName.Equals("MTConnectDevices"))
            {
                if (Data.prefix != null)
                {
                    root = new XElement(MTConnectNameSpace.mtDevices + elementName,                        
                    new XAttribute(XNamespace.Xmlns + Data.prefix, "urn:mtconnect.org:MTConnectDevices:1.1"), new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectDevices:1.1 http://mtconnect.org/schemas/MTConnectDevices_1.1.xsd"));
                }
                else
                {
                    root = new XElement(MTConnectNameSpace.mtDevices + elementName,
                           new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectDevices:1.1 http://mtconnect.org/schemas/MTConnectDevices_1.1.xsd"));
                }
            }
            else if (elementName.Equals("MTConnectError"))
            {
                if (Data.prefix != null)
                {
                    root = new XElement(MTConnectNameSpace.mtError + elementName,                        
                new XAttribute(XNamespace.Xmlns + Data.prefix, "urn:mtconnect.org:MTConnectError:1.1"), new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectError:1.1 http://mtconnect.org/schemas/MTConnectError_1.1.xsd"));
                }
                else
                {
                    root = new XElement(MTConnectNameSpace.mtError + elementName,
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(MTConnectNameSpace.xsi + "schemaLocation", "urn:mtconnect.org:MTConnectError:1.1 http://mtconnect.org/schemas/MTConnectError_1.1.xsd"));
                }
                
            }
            return root;
        }
       
        

        public static string datePatt = "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz"; 
        public static string GetDateTime()
        {
            //2008-04-29T12:34:41-07:00
            return DateTimeOffset.Now.ToString(datePatt);
        }
        public static string datePatt2 = "yyyy'-'MM'-'dd"; 
        public static string GetToday()
        {
            return DateTime.Now.ToString(datePatt2);
        }
    }
}
