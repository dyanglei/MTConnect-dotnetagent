using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

namespace MTConnectAgentCore
{
     public class Agent : IMachineAPI
     { 
         protected Data data;
         internal HttpServer hst;
         string exeFilePath;
         public Configuration config;
         public Agent()
         {
            data = new Data();
            this.exeFilePath = Assembly.GetExecutingAssembly().Location;
            config = new Configuration(true); //LOAD_DEFAULT_CONFIGURATION = true
            LogToFile.Initialize(config.UseLogFile);
            data.currentXSLTHREF = config.CurrentXSLTHREF;
            data.probeXSLTHREF = config.ProbeXSLTHREF;
            data.errorXSLTHREF = config.ErrorXSLTHREF;
            
         }

         //ReturnValue.ERROR is
         //1) when Devices.xml could not be loaded 
         //2) No Header found in Devices.xml
         //3) No bufferSize, sender, or version in Header Element
         

         public virtual void Start()
         {
             try
             {
                /* DeviceEntry ldap = new DeviceEntry(this.config);
                 LogToFile.Log("LDAP DeviceEntry Starting");
                 ldap.LDAPDeviceEntry();
                 LogToFile.Log("LDAP DeviceEntry Finished");*/

                 if (data.loadConfig() == ReturnValue.ERROR)
                 {
                     LogToFile.Log("Agent Start Failed.\n Problem in Devices.xml");
                     throw new AgentException("Agent Start Failed.\n Problem in Devices.xml");
                 }
                 //to check Devices.xml about nativeUnits and Units.
                 //data.checkConfig(); //throw exception
                 hst = new HttpServer(data);
                 hst.Start();
             }
             catch (AgentException e)
             {
                 if ( e.InnerException != null )
                    LogToFile.Log(e.Message + e.InnerException.Message);
                 else
                     LogToFile.Log(e.Message);
                 throw e;
             }
             catch (System.UnauthorizedAccessException eu)
             {
                 //Cannot use Local System Account
                 throw eu;
             }
             catch (Exception e)
             {
                 LogToFile.Log("Agent Start has Failed.  " + e);
                 throw new AgentException("Agent Start has Failed.  ", e);
             }
         }

         public virtual void Stop()
         {
             try
             {
                 hst.Stop();
             }
             catch (Exception e)
             {
                 LogToFile.Log("Agent Stop has Failed.  " + e);
                 throw new AgentException("Agent Stop has Failed.  ", e);
             }
         }

         public short StoreSample(String timestamp, String dataItemId, String value)
         {
             return data.StoreSample(timestamp, dataItemId, value);
         }

         public short StoreEvent(String timestamp, String dataItemId, String value, String code, String nativeCode)
         {
             return data.StoreEvent(timestamp, dataItemId, value, code, nativeCode);
         }

         public short StoreCondition(String timestamp, String dataItemId, String condition, String value, String nativeCode, String code)
         {
             return data.StoreCondition(timestamp, dataItemId, condition, value, nativeCode,code);
         }

     }
}
