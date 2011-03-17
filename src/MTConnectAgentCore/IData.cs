

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MTConnectAgentCore
{
    public interface IData
    {
        short loadConfig();
        String getSender();
        String getVersion();
        
        //http://xxx/probe
        short getProbe(StreamWriter writer);
        //http://xxx/deviceId/probe
        short getProbeDevice(String deviceId, StreamWriter writer);
        //http://xxx/current
        short getCurrent(StreamWriter writer);
        //http://xxx/current?path=....
        short getCurrent(String xpath, StreamWriter writer, String at, String frequency);
        //http://xxx/deiceId/current
        short getCurrentDevice(String deviceId, StreamWriter writer);
        //http://xxx/deiceId/current?path=
        short getCurrentDevice(String deviceId, String xpath, StreamWriter writer, String at, String frequency);

        String[] getDevices();
       // bool isEvent(String _name, String _device); //used by AgentSHDR
       // bool isAlarm(String _name, String _device); //used by AgentSHDR

        short getStream(StreamWriter writer); //http://xxx/sample
        short getStream(String xpath, String from, String count, StreamWriter writer, String frequency);
        short getStreamDevice(String deviceId, StreamWriter writer);
        short getStreamDevice(String deviceId, String xpath, String from, String count, StreamWriter writer, String frequency);

        short StoreSample(String timestamp, String dataItemId, String value);
        short StoreEvent(String timestamp, String dataItemId, String value, String code, String nativeCode);
        short StoreCondition(String _timestamp, String _dataItemId, String _condition, String _value, String _nativeCode, String _code);
        short getDebug(StreamWriter writer);

        //DAF 2008-07-31 Added 
        short getVersion(StreamWriter writer);
        short getLog(StreamWriter writer);
        short getConfig(StreamWriter writer);

        //void checkConfig();
     }
}
